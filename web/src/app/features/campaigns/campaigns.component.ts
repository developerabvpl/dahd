import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { CampaignsService } from '../../core/campaigns/campaigns.service';
import { ProcurementCampaign } from '../../core/campaigns/campaigns.models';
import { Warehouse } from '../../core/models';

@Component({
  selector: 'app-campaigns',
  imports: [CommonModule, FormsModule],
  templateUrl: './campaigns.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CampaignsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly svc = inject(CampaignsService);
  private readonly destroy$ = new Subject<void>();

  readonly campaigns = signal<ProcurementCampaign[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly draftSource = signal<Record<string, string>>({});
  readonly draftQty = signal<Record<string, number>>({});

  readonly canDraft = () => this.auth.hasRole('Admin', 'Director', 'Cvo');

  ngOnInit(): void {
    forkJoin({
      list: this.svc.list(),
      whs: this.api.getWarehouses()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ list, whs }) => {
          this.campaigns.set(list);
          this.warehouses.set(whs);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  centralOrDivisional(): Warehouse[] {
    return this.warehouses().filter(w => w.type === 'Central' || w.type === 'Divisional');
  }

  setSource(id: string, v: string): void { this.draftSource.update(m => ({ ...m, [id]: v })); }
  setQty(id: string, v: number): void    { this.draftQty.update(m => ({ ...m, [id]: v })); }

  windowState(c: ProcurementCampaign): { cls: string; text: string } {
    if (c.daysToWindowStart > 0)  return { cls: 'warn', text: `T-${c.daysToWindowStart}d to window` };
    if (c.daysToWindowStart <= 0 && this.daysToWindowEnd(c) >= 0) return { cls: 'bad', text: 'In-window NOW' };
    return { cls: 'ok', text: 'Window closed' };
  }

  daysToWindowEnd(c: ProcurementCampaign): number {
    const today = new Date(); today.setHours(0,0,0,0);
    const end = new Date(c.windowEnd);
    return Math.round((end.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  }

  draft(c: ProcurementCampaign): void {
    const sourceWarehouseId = this.draftSource()[c.id];
    const quantityPerDestination = Number(this.draftQty()[c.id] ?? 0);
    if (!sourceWarehouseId) { this.error.set('Pick a source warehouse.'); return; }
    if (!quantityPerDestination || quantityPerDestination <= 0) { this.error.set('Enter a positive quantity.'); return; }

    this.busyId.set(c.id);
    this.error.set(null); this.notice.set(null);
    this.svc.draftIndents(c.id, { sourceWarehouseId, quantityPerDestination })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => {
          this.busyId.set(null);
          this.notice.set(`Created ${r.indentsCreated} draft indents (total ${r.totalQuantityRequested} doses).`);
          this.svc.list().pipe(takeUntil(this.destroy$)).subscribe(list => this.campaigns.set(list));
        },
        error: e => { this.busyId.set(null); this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Draft failed'); }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
