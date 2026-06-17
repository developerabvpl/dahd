import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { CampaignsService } from '../../core/campaigns/campaigns.service';
import { DashboardKpi } from '../../core/models';
import { ProcurementCampaign } from '../../core/campaigns/campaigns.models';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly campaigns = inject(CampaignsService);
  private readonly destroy$ = new Subject<void>();

  readonly kpi = signal<DashboardKpi | null>(null);
  readonly upcoming = signal<ProcurementCampaign[]>([]);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    forkJoin({
      kpi: this.api.getKpis(),
      upcoming: this.campaigns.upcoming(3)
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ kpi, upcoming }) => {
          this.kpi.set(kpi);
          this.upcoming.set(upcoming);
          this.loading.set(false);
        },
        error: e => { this.error.set(e?.message ?? 'Failed to load'); this.loading.set(false); }
      });
  }

  leadCls(c: ProcurementCampaign): string {
    if (c.daysToWindowStart <= 0) return 'bad';
    if (c.daysToProcurementStart <= 0) return 'warn';
    return '';
  }

  leadText(c: ProcurementCampaign): string {
    if (c.daysToWindowStart <= 0) return 'In window NOW';
    if (c.daysToProcurementStart <= 0) return `Procurement open · T-${c.daysToWindowStart}d to window`;
    return `T-${c.daysToProcurementStart}d to procurement open`;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
