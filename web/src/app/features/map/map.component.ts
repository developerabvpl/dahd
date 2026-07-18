import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, NgZone,
  OnDestroy, computed, inject, signal, viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import { MapService } from '../../core/map/map.service';
import {
  ColdChainUnit, FieldForce, GeoNetwork, Store, UnitStatus, VaccineStock
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
  private storeLayer = L.layerGroup();
  private fieldLayer = L.layerGroup();

  readonly net: GeoNetwork = this.svc.network();

  readonly selectedStore = signal<Store | null>(null);
  readonly selectedField = signal<FieldForce | null>(null);
  readonly showStores = signal(true);
  readonly showField = signal(true);

  readonly storeAlarms = computed(() => this.net.stores.filter(s => s.alarms > 0).length);
  readonly fieldAlerts = computed(() => this.net.fieldForce.filter(f => f.status === 'Cold-chain alert').length);

  ngAfterViewInit(): void {
    this.zone.runOutsideAngular(() => this.initMap());
  }

  private initMap(): void {
    const map = L.map(this.mapEl().nativeElement, { center: [27.2, 80.6], zoom: 7, preferCanvas: true });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    for (const s of this.net.stores) {
      const color = s.alarms > 0 ? '#dc2626' : s.type === 'District Vaccine Store' ? '#1d4ed8' : '#0891b2';
      L.marker([s.lat, s.lng], { icon: this.pin(color, s.type === 'District Vaccine Store' ? 'D' : 'B') })
        .on('click', () => this.zone.run(() => this.openStore(s)))
        .bindTooltip(`${s.name}`, { direction: 'top' })
        .addTo(this.storeLayer);
    }
    for (const f of this.net.fieldForce) {
      const color = f.status === 'Cold-chain alert' ? '#dc2626' : f.status === 'Idle' ? '#9ca3af' : '#16a34a';
      L.marker([f.lat, f.lng], { icon: this.pin(color, '◆', true) })
        .on('click', () => this.zone.run(() => this.openField(f)))
        .bindTooltip(`${f.name} — ${f.role}`, { direction: 'top' })
        .addTo(this.fieldLayer);
    }
    this.storeLayer.addTo(map);
    this.fieldLayer.addTo(map);
    this.map = map;
    setTimeout(() => map.invalidateSize(), 200);
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

  openStore(s: Store): void { this.selectedField.set(null); this.selectedStore.set(s); this.map?.flyTo([s.lat, s.lng], 10, { duration: 0.6 }); }
  openField(f: FieldForce): void { this.selectedStore.set(null); this.selectedField.set(f); this.map?.flyTo([f.lat, f.lng], 10, { duration: 0.6 }); }
  closePanel(): void { this.selectedStore.set(null); this.selectedField.set(null); }

  toggleStores(): void {
    this.showStores.update(v => !v);
    if (!this.map) return;
    this.showStores() ? this.storeLayer.addTo(this.map) : this.map.removeLayer(this.storeLayer);
  }
  toggleField(): void {
    this.showField.update(v => !v);
    if (!this.map) return;
    this.showField() ? this.fieldLayer.addTo(this.map) : this.map.removeLayer(this.fieldLayer);
  }

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

  ngOnDestroy(): void { this.map?.remove(); }
}
