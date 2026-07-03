import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ParLevelsService } from '../../core/par-levels/par-levels.service';
import { ParLevelRow } from '../../core/par-levels/par-levels.models';
import { Drug, Warehouse } from '../../core/models';

@Component({
  selector: 'app-par-levels',
  imports: [CommonModule, FormsModule],
  templateUrl: './par-levels.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ParLevelsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly svc = inject(ParLevelsService);
  private readonly destroy$ = new Subject<void>();

  readonly rows = signal<ParLevelRow[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly drugs = signal<Drug[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly warehouseFilter = signal<string>('');
  readonly belowParOnly = signal(false);

  // add-par form
  readonly showAdd = signal(false);
  addForm = { warehouseId: '', drugId: '', parQuantity: 0, reorderToQuantity: undefined as number | undefined };

  // auto-indent
  readonly sourceId = signal<string>('');

  readonly canManage = computed(() => this.auth.hasRole('Admin', 'Director'));
  readonly canIndent = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));
  readonly belowParCount = computed(() => this.rows().filter(r => r.belowPar).length);

  ngOnInit(): void {
    forkJoin({
      rows: this.svc.list(),
      warehouses: this.api.getWarehouses(),
      drugs: this.api.getDrugs()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ rows, warehouses, drugs }) => {
          this.rows.set(rows); this.warehouses.set(warehouses); this.drugs.set(drugs); this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  reload(): void {
    this.loading.set(true);
    this.svc.list({ warehouseId: this.warehouseFilter() || undefined, belowParOnly: this.belowParOnly() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: r => { this.rows.set(r); this.loading.set(false); }, error: () => this.loading.set(false) });
  }

  toggleAdd(): void {
    this.showAdd.update(v => !v);
    if (this.showAdd()) { this.addForm = { warehouseId: this.warehouseFilter(), drugId: '', parQuantity: 0, reorderToQuantity: undefined }; }
  }

  save(): void {
    const f = this.addForm;
    if (!f.warehouseId || !f.drugId || f.parQuantity < 0) { this.error.set('Warehouse, drug and a non-negative par are required.'); return; }
    this.busy.set(true); this.error.set(null); this.notice.set(null);
    this.svc.upsert({ warehouseId: f.warehouseId, drugId: f.drugId, parQuantity: f.parQuantity, reorderToQuantity: f.reorderToQuantity })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busy.set(false); this.notice.set('Par level saved.'); this.showAdd.set(false); this.reload(); },
        error: e => { this.busy.set(false); this.error.set(this.msg(e)); }
      });
  }

  remove(r: ParLevelRow): void {
    this.busy.set(true); this.error.set(null); this.notice.set(null);
    this.svc.remove(r.id).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busy.set(false); this.notice.set(`Par level for ${r.drugCode} removed.`); this.reload(); },
        error: e => { this.busy.set(false); this.error.set(this.msg(e)); }
      });
  }

  autoIndent(): void {
    const recipientWarehouseId = this.warehouseFilter();
    const sourceWarehouseId = this.sourceId();
    if (!recipientWarehouseId) { this.error.set('Pick a warehouse (filter) to reorder for.'); return; }
    if (!sourceWarehouseId) { this.error.set('Pick a source warehouse to fulfil the auto-indent.'); return; }
    this.busy.set(true); this.error.set(null); this.notice.set(null);
    this.svc.autoIndent({ recipientWarehouseId, sourceWarehouseId })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => {
          this.busy.set(false);
          this.notice.set(r.lineCount === 0
            ? 'Nothing below par — no indent created.'
            : `Draft ${r.indentNumber} created with ${r.lineCount} below-par line(s), total ${r.totalQuantity}. Review it on Indents.`);
        },
        error: e => { this.busy.set(false); this.error.set(this.msg(e)); }
      });
  }

  private msg(e: any): string {
    return e?.error?.title ?? (typeof e?.error === 'string' ? e.error : null) ?? e?.message ?? 'Action failed';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
