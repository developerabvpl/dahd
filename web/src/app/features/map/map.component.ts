import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, NgZone,
  OnDestroy, computed, inject, signal, viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import 'leaflet.markercluster';
import { MapService } from '../../core/map/map.service';
import { ColdChainUnit, FieldForce, GeoNetwork, MapWarehouse, Store } from '../../core/map/map.models';

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

  private healthByCanon = new Map<string, number>();
  private centroids = new Map<string, [number, number]>();
  private liveByCanon = new Map<string, MapWarehouse>();
  private liveMarkerByCanon = new Map<string, L.Marker>();

  private readonly popupOpts: L.PopupOptions = { maxWidth: 520, minWidth: 420, maxHeight: 460, className: 'dahd-info', autoPan: true };

  readonly net: GeoNetwork = this.svc.network();

  readonly showStores = signal(true);
  readonly showField = signal(true);
  readonly showShading = signal(true);
  readonly showLive = signal(true);
  readonly liveCount = signal<number | null>(null);
  readonly liveError = signal<string | null>(null);

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
        .bindTooltip(s.name, { direction: 'top' })
        .bindPopup(() => this.storePopup(s), this.popupOpts)
        .addTo(this.storeCluster);
    }
    for (const f of this.net.fieldForce) {
      const color = f.status === 'Cold-chain alert' ? '#dc2626' : f.status === 'Idle' ? '#9ca3af' : '#16a34a';
      L.marker([f.lat, f.lng], { icon: this.pin(color, '◆', true) })
        .bindTooltip(`${f.name} — ${f.role}`, { direction: 'top' })
        .bindPopup(() => this.fieldPopup(f), this.popupOpts)
        .addTo(this.fieldCluster);
    }
    this.storeCluster.addTo(map);
    this.fieldCluster.addTo(map);
    this.liveCluster.addTo(map);

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
              const m = this.liveMarkerByCanon.get(canon);
              if (m) this.liveCluster.zoomToShowLayer(m, () => m.openPopup());
            });
          }
        });
        this.boundaryLayer = layer;
        if (this.map && this.showShading()) layer.addTo(this.map);
        layer.bringToBack();
        this.loadLive();
      },
      error: () => { this.zone.run(() => this.liveError.set('District boundaries failed to load; placing markers by point.')); this.loadLive(); }
    });
  }

  private loadLive(): void {
    this.svc.liveWarehouses().subscribe({
      next: whs => {
        let placed = 0;
        const perCanon = new Map<string, number>();
        for (const w of whs) {
          const canon = MapService.canon(w.district || w.division || w.name);
          const base = this.centroids.get(canon) ?? this.pointFallback(w);
          if (!base) continue;
          const dup = perCanon.get(canon) ?? 0;
          perCanon.set(canon, dup + 1);
          const cen: [number, number] = dup ? [base[0] + 0.06 * dup, base[1] + 0.06 * dup] : base;
          w.lat = cen[0]; w.lng = cen[1];
          this.liveByCanon.set(canon, w);
          const m = L.marker([w.lat, w.lng], { icon: this.pin('#d97706', '★') })
            .bindTooltip(`LIVE · ${w.name}`, { direction: 'top' })
            .bindPopup(() => this.livePopup(w), this.popupOpts)
            .addTo(this.liveCluster);
          if (dup === 0) this.liveMarkerByCanon.set(canon, m);
          placed++;
        }
        this.boundaryLayer?.setStyle((feat: any) => this.districtStyle(MapService.canon(feat?.properties?.district)));
        this.zone.run(() => this.liveCount.set(placed));
      },
      error: () => this.zone.run(() => this.liveError.set('Could not load live warehouses (backend offline?).'))
    });
  }

  // ---------- info-window (popup) HTML builders ----------

  private num(n: number): string { return Number(n).toLocaleString('en-IN'); }
  private badge(text: string, cls = ''): string { return `<span class="badge ${cls}">${text}</span>`; }
  private unitCls(s: string): string { return s === 'alarm' ? 'bad' : s === 'warn' ? 'warn' : 'ok'; }
  private tempColor(s: string): string { return s === 'alarm' ? '#dc2626' : s === 'warn' ? '#b45309' : '#047857'; }
  private kindLabel(k: ColdChainUnit['kind']): string {
    return k === 'DeepFreezer' ? 'Deep Freezer' : k === 'WalkInCooler' ? 'Walk-in Cooler' : 'Ice-Lined Refrigerator';
  }
  private head(title: string, sub: string): string {
    return `<div style="font-weight:700;font-size:1rem;line-height:1.2">${title}</div>
            <div style="color:#6b7d90;font-size:.78rem;margin-bottom:.4rem">${sub}</div>`;
  }
  private row(label: string, val: string): string {
    return `<div style="display:flex;justify-content:space-between;gap:.6rem;font-size:.82rem;padding:.1rem 0">
              <span style="color:#6b7d90">${label}</span><span style="text-align:right"><b>${val}</b></span></div>`;
  }
  private section(t: string): string { return `<div style="font-weight:700;font-size:.82rem;margin:.55rem 0 .3rem;color:#334155">${t}</div>`; }

  private storePopup(s: Store): string {
    const units = s.units.map(u => `
      <div style="border:1px solid #e2e8f0;border-radius:6px;padding:.35rem .45rem;margin-bottom:.3rem">
        <div style="display:flex;justify-content:space-between;align-items:center">
          <span><b>${u.name}</b> <span style="color:#6b7d90;font-size:.75rem">${this.kindLabel(u.kind)}</span></span>
          ${this.badge(u.status.toUpperCase(), this.unitCls(u.status))}
        </div>
        <div style="font-size:.8rem"><b style="color:${this.tempColor(u.status)}">${u.tempC} °C</b>
          <span style="color:#6b7d90"> · band ${u.targetMin}…${u.targetMax}°C · ${u.make}</span></div>
      </div>`).join('');

    const stock = s.units.map(u => {
      const lines = s.stock.filter(x => x.unitId === u.id);
      if (!lines.length) return '';
      const rows = lines.map(v => `<tr><td>${v.vaccine}</td><td>${v.batch}</td><td>${this.num(v.doses)}</td>
        <td>${v.expiry}</td><td>${this.badge('VVM ' + v.vvmStage, v.vvmStage >= 3 ? 'bad' : v.vvmStage === 2 ? 'warn' : 'ok')}</td></tr>`).join('');
      return `<div style="font-weight:600;font-size:.8rem;margin:.35rem 0 .15rem">${u.name} · ${u.tempC}°C</div>
        <table style="width:100%;font-size:.76rem"><thead><tr><th>Vaccine</th><th>Batch</th><th>Doses</th><th>Expiry</th><th>VVM</th></tr></thead><tbody>${rows}</tbody></table>`;
    }).join('');

    return `<div>
      ${this.head(s.name, `${s.code} · ${s.district}, UP`)}
      <div style="margin-bottom:.35rem">${this.badge(s.type, s.type === 'District Vaccine Store' ? 'ok' : '')}
        ${s.alarms > 0 ? this.badge(`${s.alarms} unit(s) in alarm`, 'bad') : this.badge('cold-chain healthy', 'ok')}</div>
      ${this.row('In-charge', s.incharge)}
      ${this.row('Phone', s.phone)}
      ${this.row('Coordinates', `${s.lat.toFixed(3)}, ${s.lng.toFixed(3)}`)}
      ${this.row('Total doses', this.num(s.totalDoses))}
      ${this.section('Cold-chain units (live sensor)')}${units}
      ${this.section('Vaccine stock — by refrigerator')}${stock}
    </div>`;
  }

  private fieldPopup(f: FieldForce): string {
    const sensors = f.toolbox.sensors.map(sn => `
      <div style="display:flex;justify-content:space-between;font-size:.82rem;padding:.1rem 0">
        <span style="color:#6b7d90">${sn.kind}</span>
        <span><b>${sn.value}</b> ${this.badge(sn.status.toUpperCase(), this.unitCls(sn.status))}</span></div>`).join('');
    const items = f.toolbox.contents.map(c => `<tr><td>${c.item}</td><td>${c.qty}</td>
      <td>${this.badge(c.ok ? 'OK' : 'CHECK', c.ok ? 'ok' : 'bad')}</td></tr>`).join('');
    const stCls = f.status === 'Cold-chain alert' ? 'bad' : f.status === 'Idle' ? '' : f.status === 'En route' ? 'warn' : 'ok';
    return `<div>
      ${this.head(f.name, `${f.role} · ${f.district}`)}
      <div style="margin-bottom:.35rem">${this.badge(f.status, stCls)}</div>
      ${this.row('Phone', f.phone)}
      ${this.row('Vehicle', f.vehicle)}
      ${this.row("Today's vaccinations", String(f.todaysVaccinations))}
      ${this.row('Coordinates', `${f.lat.toFixed(3)}, ${f.lng.toFixed(3)}`)}
      ${this.section(`Toolbox · ${f.toolbox.code}`)}
      <div style="color:#6b7d90;font-size:.76rem;margin-bottom:.2rem">${f.toolbox.model}</div>
      ${sensors}
      ${this.section('Toolbox contents')}
      <table style="width:100%;font-size:.78rem"><thead><tr><th>Item</th><th>Qty</th><th>Status</th></tr></thead><tbody>${items}</tbody></table>
    </div>`;
  }

  private livePopup(w: MapWarehouse): string {
    const units = w.coldChainUnits.map(u =>
      `<tr><td>${u.assetTag}</td><td>${u.name}</td><td>${this.badge(u.status, u.status === 'Active' ? 'ok' : u.status === 'BreakdownReported' ? 'bad' : 'warn')}</td></tr>`).join('');
    const stock = w.stock.map(v => {
      const exp = v.daysToExpiry < 0 ? this.badge('expired', 'bad') : v.daysToExpiry <= 90 ? this.badge(v.daysToExpiry + 'd', 'warn') : '';
      const cc = v.coldChainRequired
        ? this.badge(v.storageTempMin != null ? `${v.storageTempMin}…${v.storageTempMax}°C` : 'cold-chain', 'warn')
        : '<span style="color:#94a3b8">ambient</span>';
      return `<tr><td>${v.drug}${v.isVaccine ? ' ' + this.badge('vaccine', 'ok') : ''}</td><td>${v.batch}</td>
        <td>${this.num(v.quantity)} ${v.unit}</td><td>${v.expiry} ${exp}</td><td>${cc}</td></tr>`;
    }).join('');
    return `<div>
      ${this.head(w.name, `${w.code} · ${w.district || w.division || 'UP'}`)}
      <div style="margin-bottom:.35rem">${this.badge('LIVE · backend', 'ok')} ${this.badge(w.type)}${w.coldChainCapable ? ' ' + this.badge('cold-chain capable') : ''}</div>
      ${this.row('In-charge', w.incharge || '—')}
      ${this.row('Phone', w.phone || '—')}
      ${this.row('Total stock', `${this.num(w.totalStock)} (${w.stockLines} lines)`)}
      ${w.coldChainUnits.length ? this.section('Cold-chain equipment (Asset register)') +
        `<table style="width:100%;font-size:.78rem"><thead><tr><th>Tag</th><th>Name</th><th>Status</th></tr></thead><tbody>${units}</tbody></table>` : ''}
      ${this.section('Live stock (batches in store)')}
      ${w.stock.length
        ? `<table style="width:100%;font-size:.76rem"><thead><tr><th>Item</th><th>Batch</th><th>Qty</th><th>Expiry</th><th>Cold-chain</th></tr></thead><tbody>${stock}</tbody></table>`
        : '<div style="color:#94a3b8;font-size:.8rem">No batches in store.</div>'}
    </div>`;
  }

  // ---------- layers / helpers ----------

  private districtStyle(canon: string): L.PathOptions {
    const ratio = this.healthByCanon.get(canon);
    const fill = ratio == null ? '#cbd5e1' : ratio <= 0 ? '#16a34a' : ratio < 0.34 ? '#84cc16' : ratio < 0.67 ? '#f59e0b' : '#dc2626';
    const hasLive = this.liveByCanon.has(canon);
    return {
      color: hasLive ? '#b45309' : '#64748b', weight: hasLive ? 2.5 : 0.7,
      fillColor: fill, fillOpacity: ratio == null ? 0.12 : 0.32, dashArray: hasLive ? undefined : '2'
    };
  }

  private centroid(geom: any): [number, number] | null {
    let sx = 0, sy = 0, n = 0;
    const walk = (c: any) => { if (typeof c[0] === 'number') { sx += c[0]; sy += c[1]; n++; } else for (const x of c) walk(x); };
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
      iconSize: [20, 20], iconAnchor: [10, diamond ? 10 : 18]
    });
  }

  private toggleLayer(on: boolean, layer?: L.Layer): void {
    if (!this.map || !layer) return;
    on ? this.map.addLayer(layer) : this.map.removeLayer(layer);
    if (on && layer === this.boundaryLayer) this.boundaryLayer?.bringToBack();
  }
  toggleStores(): void { this.showStores.update(v => !v); this.toggleLayer(this.showStores(), this.storeCluster); }
  toggleField(): void { this.showField.update(v => !v); this.toggleLayer(this.showField(), this.fieldCluster); }
  toggleLive(): void { this.showLive.update(v => !v); this.toggleLayer(this.showLive(), this.liveCluster); }
  toggleShading(): void { this.showShading.update(v => !v); this.toggleLayer(this.showShading(), this.boundaryLayer); }

  ngOnDestroy(): void { this.map?.remove(); }
}
