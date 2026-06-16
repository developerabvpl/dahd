import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DashboardKpi } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly kpi = signal<DashboardKpi | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getKpis()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: k => { this.kpi.set(k); this.loading.set(false); },
        error: e => { this.error.set(e?.message ?? 'Failed to load'); this.loading.set(false); }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
