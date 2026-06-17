import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ColdChainAnalyticsService } from '../../core/coldchain-analytics/coldchain-analytics.service';
import {
  BreachHourMatrixCell, DeviceAnalyticsRow
} from '../../core/coldchain-analytics/coldchain-analytics.models';
import { Warehouse } from '../../core/models';

@Component({
  selector: 'app-coldchain-analytics',
  imports: [CommonModule, FormsModule],
  templateUrl: './coldchain-analytics.component.html',
  styleUrl: './coldchain-analytics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColdchainAnalyticsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly svc = inject(ColdChainAnalyticsService);
  private readonly destroy$ = new Subject<void>();

  readonly devices = signal<DeviceAnalyticsRow[]>([]);
  readonly heatmap = signal<BreachHourMatrixCell[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly days = signal(30);
  readonly warehouseId = signal<string>('');

  readonly dows = [1, 2, 3, 4, 5, 6, 7];
  readonly hours = Array.from({ length: 24 }, (_, i) => i);
  readonly dowLabels = ['', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  readonly heatmapMax = computed(() => Math.max(0, ...this.heatmap().map(c => c.breachCount)));
  readonly heatmapLookup = computed(() => {
    const m = new Map<string, number>();
    for (const c of this.heatmap()) m.set(`${c.dayOfWeek}-${c.hour}`, c.breachCount);
    return m;
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    const wid = this.warehouseId() || undefined;
    forkJoin({
      devices: this.svc.byDevice(this.days(), wid),
      heatmap: this.svc.breachHeatmap(this.days(), wid),
      whs: this.api.getWarehouses()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ devices, heatmap, whs }) => {
          this.devices.set(devices);
          this.heatmap.set(heatmap);
          this.warehouses.set(whs);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  cellCount(dow: number, hour: number): number {
    return this.heatmapLookup().get(`${dow}-${hour}`) ?? 0;
  }

  cellShade(dow: number, hour: number): string {
    const max = this.heatmapMax();
    if (max === 0) return 'transparent';
    const n = this.cellCount(dow, hour);
    if (n === 0) return '#f1f5f9';
    const intensity = Math.min(1, n / max);
    const alpha = 0.15 + intensity * 0.7;
    return `rgba(185, 28, 28, ${alpha.toFixed(2)})`;
  }

  oosCls(pct: number): string {
    if (pct >= 10) return 'bad';
    if (pct >= 2) return 'warn';
    return 'ok';
  }

  mktCls(mkt: number): string {
    if (mkt < 2 || mkt > 8) return 'bad';
    if (mkt < 3 || mkt > 7) return 'warn';
    return 'ok';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
