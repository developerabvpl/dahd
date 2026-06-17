import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ConsumptionService } from '../../core/consumption/consumption.service';
import { ConsumptionForecastRow } from '../../core/consumption/consumption.models';
import { Warehouse } from '../../core/models';

@Component({
  selector: 'app-consumption',
  imports: [CommonModule, FormsModule],
  templateUrl: './consumption.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConsumptionComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly svc = inject(ConsumptionService);
  private readonly destroy$ = new Subject<void>();

  readonly rows = signal<ConsumptionForecastRow[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly notice = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly warehouseFilter = signal<string>('');
  readonly recipientId = signal<string>('');
  readonly sourceId = signal<string>('');
  readonly lookbackDays = signal(365);
  readonly forecastDays = signal(90);
  readonly safety = signal(1.15);

  readonly canDraft = computed(() =>
    this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));

  readonly shortfallRows = computed(() => this.rows().filter(r => r.shortfall > 0));
  readonly shortfallTotal = computed(() => this.shortfallRows().reduce((a, r) => a + r.shortfall, 0));

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      rows: this.svc.forecast({
        lookbackDays: this.lookbackDays(),
        forecastDays: this.forecastDays(),
        safetyMultiplier: this.safety()
      }),
      whs: this.api.getWarehouses()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ rows, whs }) => {
          this.rows.set(rows);
          this.warehouses.set(whs);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  rerunForecast(): void {
    this.loading.set(true);
    this.svc.forecast({
      warehouseId: this.warehouseFilter() || undefined,
      lookbackDays: this.lookbackDays(),
      forecastDays: this.forecastDays(),
      safetyMultiplier: this.safety()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: rows => { this.rows.set(rows); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  sourceCandidates(): Warehouse[] {
    return this.warehouses().filter(w => w.type === 'Central' || w.type === 'Divisional');
  }

  recipientCandidates(): Warehouse[] {
    const srcId = this.sourceId();
    return this.warehouses().filter(w => w.id !== srcId && w.isActive);
  }

  draftQuarterly(): void {
    if (!this.recipientId() || !this.sourceId()) {
      this.error.set('Pick both a recipient and a source warehouse.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);
    this.svc.draftQuarterly({
      recipientWarehouseId: this.recipientId(),
      sourceWarehouseId: this.sourceId(),
      lookbackDays: this.lookbackDays(),
      forecastDays: this.forecastDays(),
      safetyMultiplier: this.safety()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => {
          this.busy.set(false);
          if (r.lineCount === 0) {
            this.notice.set('No shortfalls — recipient is well-stocked for the forecast window.');
          } else {
            this.notice.set(`Draft ${r.indentNumber} created with ${r.lineCount} line(s), total ${r.totalQuantity} units.`);
          }
        },
        error: e => {
          this.busy.set(false);
          this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Draft failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
