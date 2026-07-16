import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { AssetsService } from '../../core/assets/assets.service';
import { ApiService } from '../../core/api.service';
import {
  ASSET_CATEGORIES, ASSET_STATUSES, Asset, AssetCategory, AssetKpi, AssetStatus,
  CreateAmcRequest, CreateAssetRequest, CreateScheduleRequest
} from '../../core/assets/assets.models';
import { Facility, Warehouse } from '../../core/models';

@Component({
  selector: 'app-assets',
  imports: [CommonModule, FormsModule],
  templateUrl: './assets.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetsComponent implements OnInit, OnDestroy {
  private readonly svc = inject(AssetsService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly assets = signal<Asset[]>([]);
  readonly kpi = signal<AssetKpi | null>(null);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly statusFilter = signal<AssetStatus | ''>('');
  readonly categoryFilter = signal<AssetCategory | ''>('');
  readonly breakdownInput = signal<Record<string, string>>({});

  readonly categories = ASSET_CATEGORIES;
  readonly statuses = ASSET_STATUSES;

  readonly warehouses = signal<Warehouse[]>([]);
  readonly facilities = signal<Facility[]>([]);

  // add-asset form
  readonly showCreate = signal(false);
  draft: CreateAssetRequest = this.blankAsset();

  // per-asset inline forms: 'schedule' | 'amc' | null
  readonly inlineFormId = signal<string | null>(null);
  readonly inlineFormKind = signal<'schedule' | 'amc' | null>(null);
  scheduleDraft: CreateScheduleRequest = { taskDescription: '', frequencyDays: 90 };
  amcDraft: CreateAmcRequest = this.blankAmc();

  readonly canAct = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge', 'FacilityVet', 'MvuVet'));
  readonly canManage = computed(() => this.auth.hasRole('Admin', 'Director'));
  readonly canSchedule = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));

  private readonly api = inject(ApiService);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    forkJoin({
      assets: this.svc.list({
        status: this.statusFilter() || undefined,
        category: this.categoryFilter() || undefined
      }),
      kpi: this.svc.kpis(),
      warehouses: this.api.getWarehouses(),
      facilities: this.api.getFacilities()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ assets, kpi, warehouses, facilities }) => {
          this.assets.set(assets); this.kpi.set(kpi);
          this.warehouses.set(warehouses); this.facilities.set(facilities);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  blankAsset(): CreateAssetRequest {
    return {
      assetTag: '', name: '', category: 'ColdChainEquipment', model: '', serialNumber: '',
      manufacturer: '', warehouseId: undefined, facilityId: undefined, locationNote: '',
      purchaseDate: undefined, purchaseCost: undefined, warrantyUntil: undefined,
      condition: 'New', notes: ''
    };
  }

  blankAmc(): CreateAmcRequest {
    const today = new Date().toISOString().slice(0, 10);
    const nextYear = new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10);
    return { contractNumber: '', vendorName: '', startDate: today, endDate: nextYear, annualCost: 0, coverage: '' };
  }

  toggleCreate(): void {
    this.showCreate.update(v => !v);
    if (this.showCreate()) { this.draft = this.blankAsset(); this.error.set(null); this.notice.set(null); }
  }

  createAsset(): void {
    const d = this.draft;
    if (!d.assetTag || !d.name) { this.error.set('Asset tag and name are required.'); return; }
    if (!d.warehouseId && !d.facilityId && !d.locationNote) { this.error.set('Give the asset a location (warehouse, facility, or note).'); return; }
    this.busyId.set('create');
    this.error.set(null); this.notice.set(null);
    this.svc.create({ ...d, warehouseId: d.warehouseId || undefined, facilityId: d.facilityId || undefined })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: a => { this.busyId.set(null); this.notice.set(`Asset ${a.assetTag} registered.`); this.showCreate.set(false); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(this.msg(e)); }
      });
  }

  openInline(a: Asset, kind: 'schedule' | 'amc'): void {
    this.inlineFormId.set(a.id);
    this.inlineFormKind.set(kind);
    if (kind === 'schedule') this.scheduleDraft = { taskDescription: '', frequencyDays: 90 };
    else this.amcDraft = this.blankAmc();
    this.error.set(null); this.notice.set(null);
  }

  closeInline(): void { this.inlineFormId.set(null); this.inlineFormKind.set(null); }

  isInline(a: Asset, kind: 'schedule' | 'amc'): boolean {
    return this.inlineFormId() === a.id && this.inlineFormKind() === kind;
  }

  saveSchedule(a: Asset): void {
    if (!this.scheduleDraft.taskDescription || this.scheduleDraft.frequencyDays <= 0) {
      this.error.set('Task description and a positive frequency are required.'); return;
    }
    this.busyId.set(a.id);
    this.svc.addSchedule(a.id, this.scheduleDraft)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`PPM schedule added to ${a.assetTag}.`); this.closeInline(); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(this.msg(e)); }
      });
  }

  saveAmc(a: Asset): void {
    const d = this.amcDraft;
    if (!d.contractNumber || !d.vendorName) { this.error.set('Contract number and vendor are required.'); return; }
    if (d.endDate < d.startDate) { this.error.set('AMC end date must be after start date.'); return; }
    this.busyId.set(a.id);
    this.svc.addAmc(a.id, d)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`AMC ${d.contractNumber} added to ${a.assetTag}.`); this.closeInline(); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(this.msg(e)); }
      });
  }

  private msg(e: any): string {
    return e?.error?.title ?? (typeof e?.error === 'string' ? e.error : null) ?? e?.message ?? 'Action failed';
  }

  setBreakdown(id: string, v: string): void { this.breakdownInput.update(m => ({ ...m, [id]: v })); }

  statusCls(s: AssetStatus): string {
    if (s === 'Active') return 'ok';
    if (s === 'UnderMaintenance') return 'warn';
    if (s === 'BreakdownReported') return 'bad';
    return '';
  }

  location(a: Asset): string {
    return a.warehouseName || a.facilityName || a.locationNote || '—';
  }

  logBreakdown(a: Asset): void {
    const desc = (this.breakdownInput()[a.id] ?? '').trim();
    if (!desc) { this.error.set('Describe the breakdown first.'); return; }
    this.busyId.set(a.id);
    this.error.set(null); this.notice.set(null);
    this.svc.logBreakdown(a.id, { description: desc })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: j => { this.busyId.set(null); this.notice.set(`Breakdown ${j.jobNumber} logged. See Maintenance to action it.`); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Failed'); }
      });
  }

  condemn(a: Asset): void {
    this.busyId.set(a.id);
    this.error.set(null); this.notice.set(null);
    this.svc.updateStatus(a.id, 'Condemned', undefined, 'Condemned via asset register')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`${a.assetTag} marked Condemned.`); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(e?.error?.title ?? e?.message ?? 'Failed'); }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
