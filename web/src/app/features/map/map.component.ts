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
  private shadingLayer = L.layerGroup();

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

  readonly storeAlarms = computed(() => this.net.stores.filter(s => s.alarms > 0).length);
  readonly fieldAlerts = computed(() => this.net.fieldForce.filter(f => f.status === 'Cold-chain alert').length);

  ngAfterViewInit(): void {
    this.zone.runOutsideAngular(() => this.initMap());
    this.loadLive();
  }

  private initMap(): void {
    const map = L.map(this.mapEl().nativeElement, { center: [27.2, 80.6], zoom: 7, preferCanvas: true });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18, attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    // District shading (choropleth by cold-chain alarm ratio) — drawn under the markers.
    for (const d of this.svc.districtShades()) {
      const color = this.shadeColor(d.alarmRatio);
      L.circle([d.lat, d.lng], {
        radius: 15000, color, weight: 1, fillColor: color, fillOpacity: 0.22, interactive: true
      })
        .bindTooltip(`${d.district} — ${d.stores} stores, ${d.alarms}/${d.units} units in alarm`, { sticky: true })
        .addTo(this.shadingLayer);
    }

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

    this.shadingLayer.addTo(map);
    this.storeCluster.addTo(map);
    this.fieldCluster.addTo(map);
    this.liveCluster.addTo(map);
    this.map = map;
    setTimeout(() => map.invalidateSize(), 200);
  }

  private loadLive(): void {
    this.svc.liveWarehouses().subscribe({
      next: whs => {
        let placed = 0;
        for (const w of whs) {
          const g = this.svc.geocode(w.district, w.name);
          if (!g) continue;
          // small deterministic jitter so co-district warehouses don't stack exactly
          w.lat = g.lat + (placed % 2 ? 0.03 : -0.03);
          w.lng = g.lng + (placed % 3 ? 0.03 : -0.03);
          L.marker([w.lat, w.lng], { icon: this.pin('#d97706', '★') })
            .on('click', () => this.zone.run(() => this.openLive(w)))
            .bindTooltip(`LIVE · ${w.name}`, { direction: 'top' })
            .addTo(this.liveCluster);
          placed++;
        }
        this.liveCount.set(placed);
      },
      error: () => this.liveError.set('Could not load live warehouses (backend offline?).')
    });
  }

  private shadeColor(ratio: number): string {
    if (ratio <= 0) return '#16a34a';       // all healthy
    if (ratio < 0.34) return '#84cc16';      // mostly healthy
    if (ratio < 0.67) return '#f59e0b';      // watch
    return '#dc2626';                        // widespread alarm
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
  openLive(w: MapWarehouse): void { this.clear(); this.selectedLive.set(w); if (w.lat != null && w.lng != null) this.map?.flyTo([w.lat, w.lng], 10, { duration: 0.6 }); }
  closePanel(): void { this.clear(); }
  private clear(): void { this.selectedStore.set(null); this.selectedField.set(null); this.selectedLive.set(null); }

  private toggleLayer(on: boolean, layer: L.Layer): void {
    if (!this.map) return;
    on ? this.map.addLayer(layer) : this.map.removeLayer(layer);
  }
  toggleStores(): void { this.showStores.update(v => !v); this.toggleLayer(this.showStores(), this.storeCluster); }
  toggleField(): void { this.showField.update(v => !v); this.toggleLayer(this.showField(), this.fieldCluster); }
  toggleLive(): void { this.showLive.update(v => !v); this.toggleLayer(this.showLive(), this.liveCluster); }
  toggleShading(): void { this.showShading.update(v => !v); this.toggleLayer(this.showShading(), this.shadingLayer); }

  // ---- template helpers ----
  stockInUnit(store: Store, unit: ColdChainUnit): VaccineStock[] {
    return store.stock.filter(x => x.unitId === unit.id);
  }
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
