import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, NgZone,
  OnDestroy, computed, inject, signal, viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import 'leaflet.markercluster';
import { MapService } from '../../core/map/map.service';
import {
  ColdChainUnit, FieldForce, GeoNetwork, MapWarehouse, Store, UnitStatus, VaccineStock
} from '../../core/map/map.models';

@Component({
  selector: 'app-map',
  imports: [CommonModule],
  templateUrl: './map.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MapComponent implements AfterViewInit, OnDestroy {
  private readonly svc = inject(MapService);
  private readonly zone = inject(NgZone);
  private readonly mapEl = viewChild.required<ElementRef<HTMLDivElement>>('map');

  private map?: L.Map;
  private storeCluster = L.markerClusterGroup({ maxClusterRadius: 55, chunkedLoading: true });
  private fieldCluster = L.markerClusterGroup({ maxClusterRadius: 55, chunkedLoading: true });
  private liveCluster = L.markerClusterGroup({ maxClusterRadius: 45, chunkedLoading: true });
  private boundaryLayer?: L.GeoJSON;

  // district joins
  private healthByCanon = new Map<string, number>();               // canon → alarm ratio
  private centroids = new Map<string, [number, number]>();          // canon → [lat,lng]
  private liveByCanon = new Map<string, MapWarehouse>();            // canon → live warehouse

  readonly net: GeoNetwork = this.svc.network();

  readonly selectedStore = signal<Store | null>(null);
  readonly selectedField = signal<FieldForce | null>(null);
  readonly selectedLive = signal<MapWarehouse | null>(null);
  readonly showStores = signal(true);
  readonly showField = signal(true);
  readonly showShading = signal(true);
  readonly showLive = signal(true);
  readonly liveCount = signal<number | null>(null);
  readonly liveError = signal<string | null>(null);
  readonly boundariesReady = signal(false);

  readonly storeAlarms = computed(() => this.net.stores.filter(s => s.alarms > 0).length);
  readonly fieldAlerts = computed(() => this.net.fieldForce.filter(f => f.status === 'Cold-chain alert').length);

  ngAfterViewInit(): void {
    for (const d of this.svc.districtShades()) this.healthByCanon.set(MapService.canon(d.district), d.alarmRatio);
    this.zone.runOutsideAngular(() => this.initMap());
  }

  private initMap(): void {
    const map = L.map(this.mapEl().nativeElement, { center: [27.2, 80.6], zoom: 7 });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18, attribution: '© OpenStreetMap contributors'
    }).addTo(map);
    this.map = map;

    for (const s of this.net.stores) {
      const color = s.alarms > 0 ? '#dc2626' : s.type === 'District Vaccine Store' ? '#1d4ed8' : '#0891b2';
      L.marker([s.lat, s.lng], { icon: this.pin(color, s.type === 'District Vaccine Store' ? 'D' : 'B') })
        .on('click', () => this.zone.run(() => this.openStore(s)))
        .bindTooltip(s.name, { direction: 'top' })
        .addTo(this.storeCluster);
    }
    for (const f of this.net.fieldForce) {
      const color = f.status === 'Cold-chain alert' ? '#dc2626' : f.status === 'Idle' ? '#9ca3af' : '#16a34a';
      L.marker([f.lat, f.lng], { icon: this.pin(color, '◆', true) })
        .on('click', () => this.zone.run(() => this.openField(f)))
        .bindTooltip(`${f.name} — ${f.role}`, { direction: 'top' })
        .addTo(this.fieldCluster);
    }
    this.storeCluster.addTo(map);
    this.fieldCluster.addTo(map);
    this.liveCluster.addTo(map);

    // True district boundaries first (so centroids exist), then place the live markers.
    this.loadBoundaries();
    setTimeout(() => map.invalidateSize(), 200);
  }

  private loadBoundaries(): void {
    this.svc.boundaries().subscribe({
      next: fc => {
        for (const f of fc.features ?? []) {
          const c = MapService.canon(f.properties?.district);
          const cen = this.centroid(f.geometry);
          if (cen) this.centroids.set(c, cen);
        }
        const layer = L.geoJSON(fc, {
          style: (feat: any) => this.districtStyle(MapService.canon(feat?.properties?.district)),
          onEachFeature: (feat: any, lyr: L.Layer) => {
            const canon = MapService.canon(feat?.properties?.district);
            const ratio = this.healthByCanon.get(canon);
            const health = ratio == null ? 'no demo data' : ratio <= 0 ? 'healthy' : ratio < 0.5 ? 'watch' : 'alarm';
            (lyr as L.Path).bindTooltip(
              `${feat?.properties?.district ?? '—'} — cold-chain ${health}${this.liveByCanon.has(canon) ? ' · has live warehouse' : ''}`,
              { sticky: true });
            lyr.on('click', () => {
              const w = this.liveByCanon.get(canon);
              if (w) this.zone.run(() => this.openLive(w));
            });
          }
        });
        this.boundaryLayer = layer;
        if (this.map && this.showShading()) layer.addTo(this.map);
        layer.bringToBack();
        this.zone.run(() => this.boundariesReady.set(true));
        this.loadLive();
      },
      error: () => { this.liveError.set('District boundaries failed to load; placing markers by district point.'); this.loadLive(); }
    });
  }

  private loadLive(): void {
    this.svc.liveWarehouses().subscribe({
      next: whs => {
        let placed = 0;
        const perCanon = new Map<string, number>();
        for (const w of whs) {
          // district first, else division (divisions are named after their HQ district), else the store name
          const canon = MapService.canon(w.district || w.division || w.name);
          const base = this.centroids.get(canon) ?? this.pointFallback(w);
          if (!base) continue;
          const dup = perCanon.get(canon) ?? 0;
          perCanon.set(canon, dup + 1);
          // nudge co-district warehouses apart so they don't stack on the same centroid
          const cen: [number, number] = dup ? [base[0] + 0.06 * dup, base[1] + 0.06 * dup] : base;
          w.lat = cen[0]; w.lng = cen[1];
          this.liveByCanon.set(canon, w);
          L.marker([w.lat, w.lng], { icon: this.pin('#d97706', '★') })
            .on('click', () => this.zone.run(() => this.openLive(w)))
            .bindTooltip(`LIVE · ${w.name}`, { direction: 'top' })
            .addTo(this.liveCluster);
          placed++;
        }
        // re-style boundaries so districts that own a live warehouse get the gold outline
        this.boundaryLayer?.setStyle((feat: any) => this.districtStyle(MapService.canon(feat?.properties?.district)));
        this.zone.run(() => this.liveCount.set(placed));
      },
      error: () => this.zone.run(() => this.liveError.set('Could not load live warehouses (backend offline?).'))
    });
  }

  private districtStyle(canon: string): L.PathOptions {
    const ratio = this.healthByCanon.get(canon);
    const fill = ratio == null ? '#cbd5e1' : ratio <= 0 ? '#16a34a' : ratio < 0.34 ? '#84cc16' : ratio < 0.67 ? '#f59e0b' : '#dc2626';
    const hasLive = this.liveByCanon.has(canon);
    return {
      color: hasLive ? '#b45309' : '#64748b',
      weight: hasLive ? 2.5 : 0.7,
      fillColor: fill,
      fillOpacity: ratio == null ? 0.12 : 0.32,
      dashArray: hasLive ? undefined : '2'
    };
  }

  /** Rough centroid (mean of all vertices) → [lat,lng] from GeoJSON [lng,lat] geometry. */
  private centroid(geom: any): [number, number] | null {
    let sx = 0, sy = 0, n = 0;
    const walk = (c: any) => {
      if (typeof c[0] === 'number') { sx += c[0]; sy += c[1]; n++; }
      else for (const x of c) walk(x);
    };
    if (!geom?.coordinates) return null;
    walk(geom.coordinates);
    return n ? [sy / n, sx / n] : null;
  }

  private pointFallback(w: MapWarehouse): [number, number] | null {
    const g = this.svc.geocode(w.district || w.division, w.name);
    return g ? [g.lat, g.lng] : null;
  }

  private pin(color: string, glyph: string, diamond = false): L.DivIcon {
    const shape = diamond ? 'transform:rotate(45deg);border-radius:3px' : 'border-radius:50% 50% 50% 0';
    const g = diamond ? '' : glyph;
    return L.divIcon({
      className: 'dahd-pin',
      html: `<div style="width:20px;height:20px;background:${color};${shape};
              box-shadow:0 1px 4px rgba(0,0,0,.4);border:2px solid #fff;
              display:flex;align-items:center;justify-content:center;color:#fff;font:700 11px/1 sans-serif">${g}</div>`,
      iconSize: [20, 20],
      iconAnchor: [10, diamond ? 10 : 18]
    });
  }

  openStore(s: Store): void { this.clear(); this.selectedStore.set(s); this.map?.flyTo([s.lat, s.lng], 10, { duration: 0.6 }); }
  openField(f: FieldForce): void { this.clear(); this.selectedField.set(f); this.map?.flyTo([f.lat, f.lng], 10, { duration: 0.6 }); }
  openLive(w: MapWarehouse): void { this.clear(); this.selectedLive.set(w); if (w.lat != null && w.lng != null) this.map?.flyTo([w.lat, w.lng], 9, { duration: 0.6 }); }
  closePanel(): void { this.clear(); }
  private clear(): void { this.selectedStore.set(null); this.selectedField.set(null); this.selectedLive.set(null); }

  private toggleLayer(on: boolean, layer?: L.Layer): void {
    if (!this.map || !layer) return;
    on ? this.map.addLayer(layer) : this.map.removeLayer(layer);
    if (on && layer === this.boundaryLayer) this.boundaryLayer?.bringToBack();
  }
  toggleStores(): void { this.showStores.update(v => !v); this.toggleLayer(this.showStores(), this.storeCluster); }
  toggleField(): void { this.showField.update(v => !v); this.toggleLayer(this.showField(), this.fieldCluster); }
  toggleLive(): void { this.showLive.update(v => !v); this.toggleLayer(this.showLive(), this.liveCluster); }
  toggleShading(): void { this.showShading.update(v => !v); this.toggleLayer(this.showShading(), this.boundaryLayer); }

  // ---- template helpers ----
  stockInUnit(store: Store, unit: ColdChainUnit): VaccineStock[] { return store.stock.filter(x => x.unitId === unit.id); }
  unitCls(s: UnitStatus): string { return s === 'alarm' ? 'bad' : s === 'warn' ? 'warn' : 'ok'; }
  unitKindLabel(k: ColdChainUnit['kind']): string {
    return k === 'DeepFreezer' ? 'Deep Freezer' : k === 'WalkInCooler' ? 'Walk-in Cooler' : 'Ice-Lined Refrigerator';
  }
  statusCls(s: FieldForce['status']): string {
    return s === 'Cold-chain alert' ? 'bad' : s === 'Idle' ? '' : s === 'En route' ? 'warn' : 'ok';
  }
  vvmCls(stage: number): string { return stage >= 3 ? 'bad' : stage === 2 ? 'warn' : 'ok'; }
  expiryCls(days: number): string { return days < 0 ? 'bad' : days <= 90 ? 'warn' : 'ok'; }

  ngOnDestroy(): void { this.map?.remove(); }
}
