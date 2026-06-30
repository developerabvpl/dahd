import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import {
  CreateIndentLineRequest, Drug, Indent, IndentLine, IndentStatus, LineApproval, Warehouse
} from '../../core/models';

type ReviewMode = 'approve' | 'issue' | 'receive';

@Component({
  selector: 'app-indents',
  imports: [CommonModule, FormsModule],
  templateUrl: './indents.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IndentsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly indents = signal<Indent[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly drugs = signal<Drug[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly statusFilter = signal<IndentStatus | ''>('');

  // review panel state — only one indent open at a time
  readonly reviewId = signal<string | null>(null);
  readonly reviewMode = signal<ReviewMode | null>(null);
  readonly approveQty = signal<Record<string, number>>({});
  readonly sourceStock = signal<Record<string, number>>({});

  // raise-indent form
  readonly showCreate = signal(false);
  draft = this.blankDraft();

  readonly statuses: IndentStatus[] = ['Draft', 'Submitted', 'Approved', 'Issued', 'Received'];

  readonly filtered = computed(() => {
    const s = this.statusFilter();
    return s ? this.indents().filter(i => i.status === s) : this.indents();
  });

  readonly canSubmit = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));
  readonly canApprove = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo'));
  readonly canIssue = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));

  ngOnInit(): void {
    forkJoin({
      indents: this.api.getIndents(),
      warehouses: this.api.getWarehouses(),
      drugs: this.api.getDrugs()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ indents, warehouses, drugs }) => {
          this.indents.set(indents);
          this.warehouses.set(warehouses);
          this.drugs.set(drugs);
          this.loading.set(false);
        },
        error: e => { this.error.set(this.msg(e)); this.loading.set(false); }
      });
  }

  reload(): void {
    this.api.getIndents().pipe(takeUntil(this.destroy$))
      .subscribe({ next: i => this.indents.set(i), error: e => this.error.set(this.msg(e)) });
  }

  countFor(s: IndentStatus): number {
    return this.indents().filter(i => i.status === s).length;
  }

  statusBadge(s: IndentStatus): string {
    switch (s) {
      case 'Draft': return '';
      case 'Submitted': case 'Approved': case 'Issued': return 'warn';
      case 'Received': case 'Closed': return 'ok';
      case 'Rejected': return 'bad';
      default: return '';
    }
  }

  // ── Review panel ──

  isReviewing(i: Indent, mode: ReviewMode): boolean {
    return this.reviewId() === i.id && this.reviewMode() === mode;
  }

  cancelReview(): void {
    this.reviewId.set(null);
    this.reviewMode.set(null);
  }

  beginApprove(i: Indent): void {
    const map: Record<string, number> = {};
    for (const l of i.lines) map[l.id] = l.approvedQuantity ?? l.requestedQuantity;
    this.approveQty.set(map);
    this.reviewId.set(i.id);
    this.reviewMode.set('approve');
    this.error.set(null); this.notice.set(null);
  }

  setApproveQty(lineId: string, v: number): void {
    this.approveQty.update(m => ({ ...m, [lineId]: v }));
  }

  confirmApprove(i: Indent): void {
    const lineApprovals: LineApproval[] = i.lines.map(l => ({
      lineId: l.id,
      approvedQuantity: Number(this.approveQty()[l.id] ?? l.requestedQuantity)
    }));
    if (lineApprovals.some(l => l.approvedQuantity < 0)) { this.error.set('Approved quantity cannot be negative.'); return; }
    this.act(i, () => this.api.approveIndent(i.id, lineApprovals), 'approved');
  }

  beginIssue(i: Indent): void {
    this.reviewId.set(i.id);
    this.reviewMode.set('issue');
    this.error.set(null); this.notice.set(null);
    this.sourceStock.set({});
    // show available stock at the SOURCE (fulfilling) warehouse so the issuer
    // sees what FEFO will draw from before committing.
    this.api.getStockByDrug({ warehouseId: i.fulfilledByWarehouseId })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: rows => {
          const m: Record<string, number> = {};
          for (const r of rows) m[r.drugId] = r.totalQuantity;
          this.sourceStock.set(m);
        },
        error: () => this.sourceStock.set({})
      });
  }

  stockFor(drugId: string): number {
    return this.sourceStock()[drugId] ?? 0;
  }

  issuableLine(l: IndentLine): number {
    return l.approvedQuantity ?? l.requestedQuantity;
  }

  canFulfilLine(l: IndentLine): boolean {
    return this.stockFor(l.drugId) >= this.issuableLine(l);
  }

  allLinesFulfillable(i: Indent): boolean {
    return i.lines.every(l => this.canFulfilLine(l));
  }

  confirmIssue(i: Indent): void {
    this.act(i, () => this.api.issueIndent(i.id), 'issued');
  }

  beginReceive(i: Indent): void {
    this.reviewId.set(i.id);
    this.reviewMode.set('receive');
    this.error.set(null); this.notice.set(null);
  }

  confirmReceive(i: Indent): void {
    this.act(i, () => this.api.receiveIndent(i.id), 'received');
  }

  submit(i: Indent): void {
    if (i.lines.length === 0) { this.error.set('Cannot submit an empty indent.'); return; }
    this.act(i, () => this.api.submitIndent(i.id), 'submitted');
  }

  private act(i: Indent, call: () => ReturnType<ApiService['submitIndent']>, verb: string): void {
    this.busyId.set(i.id);
    this.error.set(null); this.notice.set(null);
    call().pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.busyId.set(null);
          this.notice.set(`Indent ${i.indentNumber} ${verb}.`);
          this.cancelReview();
          this.reload();
        },
        error: e => { this.busyId.set(null); this.error.set(this.msg(e)); }
      });
  }

  // ── Raise indent ──

  blankDraft(): { raisedByWarehouseId: string; fulfilledByWarehouseId: string; remarks: string; lines: CreateIndentLineRequest[] } {
    return { raisedByWarehouseId: '', fulfilledByWarehouseId: '', remarks: '', lines: [{ drugId: '', requestedQuantity: 1 }] };
  }

  toggleCreate(): void {
    this.showCreate.update(v => !v);
    if (this.showCreate()) { this.draft = this.blankDraft(); this.error.set(null); this.notice.set(null); }
  }

  addLine(): void { this.draft.lines.push({ drugId: '', requestedQuantity: 1 }); }
  removeLine(idx: number): void { this.draft.lines.splice(idx, 1); }

  createIndent(): void {
    const d = this.draft;
    if (!d.raisedByWarehouseId || !d.fulfilledByWarehouseId) { this.error.set('Pick both the raising and source warehouse.'); return; }
    if (d.raisedByWarehouseId === d.fulfilledByWarehouseId) { this.error.set('Raising and source warehouse must differ.'); return; }
    const lines = d.lines.filter(l => l.drugId && l.requestedQuantity > 0);
    if (lines.length === 0) { this.error.set('Add at least one line with a drug and quantity.'); return; }

    this.busyId.set('create');
    this.error.set(null); this.notice.set(null);
    this.api.createIndent({
      raisedByWarehouseId: d.raisedByWarehouseId,
      fulfilledByWarehouseId: d.fulfilledByWarehouseId,
      remarks: d.remarks || undefined,
      lines
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: created => {
          this.busyId.set(null);
          this.notice.set(`Indent ${created.indentNumber} created as Draft. Review its lines, then Submit.`);
          this.showCreate.set(false);
          this.reload();
        },
        error: e => { this.busyId.set(null); this.error.set(this.msg(e)); }
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
