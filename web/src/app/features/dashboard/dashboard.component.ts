import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { CampaignsService } from '../../core/campaigns/campaigns.service';
import { DashboardKpi } from '../../core/models';
import { ProcurementCampaign } from '../../core/campaigns/campaigns.models';

type RoleKind = 'director' | 'cvo' | 'warehouse' | 'vet';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly campaigns = inject(CampaignsService);
  private readonly destroy$ = new Subject<void>();

  readonly kpi = signal<DashboardKpi | null>(null);
  readonly upcoming = signal<ProcurementCampaign[]>([]);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

  readonly roleKind = computed<RoleKind>(() => {
    const r = this.auth.user()?.role;
    if (r === 'Admin' || r === 'Director') return 'director';
    if (r === 'Cvo') return 'cvo';
    if (r === 'WarehouseIncharge') return 'warehouse';
    return 'vet';
  });

  readonly roleHeading = computed(() => {
    switch (this.roleKind()) {
      case 'director':  return 'Director / Admin view — full state picture across stock, indents, cold chain, and procurement.';
      case 'cvo':       return 'CVO view — district-level facility activity, indents awaiting approval, cold-chain compliance.';
      case 'warehouse': return 'Warehouse In-Charge view — stock health, expiring batches, open indents, breach acknowledgement queue.';
      default:          return 'Vet view — dispensing activity and what stock is available to dispense.';
    }
  });

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
