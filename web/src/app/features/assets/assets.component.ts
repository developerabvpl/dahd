import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { AssetsService } from '../../core/assets/assets.service';
import {
  ASSET_CATEGORIES, ASSET_STATUSES, Asset, AssetCategory, AssetKpi, AssetStatus
} from '../../core/assets/assets.models';

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

  readonly canAct = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge', 'FacilityVet', 'MvuVet'));

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    forkJoin({
      assets: this.svc.list({
        status: this.statusFilter() || undefined,
        category: this.categoryFilter() || undefined
      }),
      kpi: this.svc.kpis()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ assets, kpi }) => { this.assets.set(assets); this.kpi.set(kpi); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
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
