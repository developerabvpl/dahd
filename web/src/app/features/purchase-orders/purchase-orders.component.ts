import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, of, takeUntil } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { PurchaseOrdersService } from '../../core/purchase-orders/purchase-orders.service';
import { RateContractsService } from '../../core/rate-contracts/rate-contracts.service';
import { VendorService } from '../../core/vendor/vendor.service';
import {
  CreatePoRequest, GrnLineRequest, PO_STATUSES, PoLine, PoStatus, PurchaseOrder
} from '../../core/purchase-orders/purchase-orders.models';
import { Vendor } from '../../core/vendor/vendor.models';
import { Drug, Warehouse } from '../../core/models';

interface GrnDraftLine {
  quantity: number;
  batchNumber: string;
  manufactureDate: string;
  expiryDate: string;
  manufacturer: string;
}

@Component({
  selector: 'app-purchase-orders',
  imports: [CommonModule, FormsModule],
  templateUrl: './purchase-orders.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PurchaseOrdersComponent implements OnInit, OnDestroy {
  private readonly svc = inject(PurchaseOrdersService);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly rc = inject(RateContractsService);
  private readonly vendorSvc = inject(VendorService);
  private readonly destroy$ = new Subject<void>();

  readonly pos = signal<PurchaseOrder[]>([]);
  readonly drugs = signal<Drug[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly vendors = signal<Vendor[]>([]);
  readonly cheapestRate = signal<Record<string, number>>({});
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly statusFilter = signal<PoStatus | ''>('');
  readonly statuses = PO_STATUSES;

  // create form
  readonly showCreate = signal(false);
  draft: CreatePoRequest = this.blankPo();

  // cancel panel
  readonly cancelId = signal<string | null>(null);
  cancelReason = '';

  // GRN panel
  readonly grnId = signal<string | null>(null);
  grnWarehouseId = '';
  grnLines: Record<string, GrnDraftLine> = {};

  readonly canCreate = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));
  readonly canIssue = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo'));

  readonly filtered = computed(() => {
    const s = this.statusFilter();
    return s ? this.pos().filter(p => p.status === s) : this.pos();
  });

  ngOnInit(): void {
    forkJoin({
      pos: this.svc.list(),
      drugs: this.api.getDrugs(),
      warehouses: this.api.getWarehouses(),
      cheapest: this.rc.cheapest().pipe(catchError(() => of([]))),
      // vendors list is admin-gated; fall back to free-text vendor for other roles
      vendors: this.vendorSvc.list('Approved').pipe(catchError(() => of([])))
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ pos, drugs, warehouses, cheapest, vendors }) => {
          this.pos.set(pos);
          this.drugs.set(drugs);
          this.warehouses.set(warehouses);
          this.vendors.set(vendors);
          const map: Record<string, number> = {};
          for (const c of cheapest) map[c.drugId] = c.unitRate;
          this.cheapestRate.set(map);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  reload(): void {
    this.svc.list().pipe(takeUntil(this.destroy$))
      .subscribe({ next: p => this.pos.set(p), error: e => this.error.set(this.msg(e)) });
  }

  countFor(s: PoStatus): number { return this.pos().filter(p => p.status === s).length; }

  statusCls(s: PoStatus): string {
    switch (s) {
      case 'Received': return 'ok';
      case 'Cancelled': return 'bad';
      case 'Draft': return '';
      default: return 'warn';
    }
  }

  blankPo(): CreatePoRequest {
    return { vendorId: undefined, vendorName: '', destinationWarehouseId: '', expectedDelivery: undefined, remarks: '', lines: [{ drugId: '', orderedQuantity: 1, unitRate: 0 }] };
  }

  toggleCreate(): void {
    this.showCreate.update(v => !v);
    if (this.showCreate()) { this.draft = this.blankPo(); this.error.set(null); this.notice.set(null); }
  }

  addLine(): void { this.draft.lines.push({ drugId: '', orderedQuantity: 1, unitRate: 0 }); }
  removeLine(i: number): void { this.draft.lines.splice(i, 1); }

  onLineDrugChange(idx: number): void {
    const l = this.draft.lines[idx];
    const rate = this.cheapestRate()[l.drugId];
    if (rate != null && (!l.unitRate || l.unitRate === 0)) l.unitRate = rate;
  }

  draftTotal(): number {
    return this.draft.lines.reduce((a, l) => a + (l.orderedQuantity || 0) * (l.unitRate || 0), 0);
  }

  createPo(): void {
    const d = this.draft;
    if (!d.vendorId && !(d.vendorName ?? '').trim()) { this.error.set('Pick an empanelled vendor or type a vendor name.'); return; }
    if (!d.destinationWarehouseId) { this.error.set('Pick the destination warehouse.'); return; }
    const lines = d.lines.filter(l => l.drugId && l.orderedQuantity > 0);
    if (lines.length === 0) { this.error.set('Add at least one line with a drug and quantity.'); return; }

    this.busyId.set('create');
    this.error.set(null); this.notice.set(null);
    this.svc.create({ ...d, vendorName: d.vendorId ? undefined : d.vendorName, lines })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: po => { this.busyId.set(null); this.notice.set(`PO ${po.poNumber} drafted (₹${po.totalAmount}). Review the lines, then Issue it to the vendor.`); this.showCreate.set(false); this.reload(); },
        error: e => { this.busyId.set(null); this.error.set(this.msg(e)); }
      });
  }

  issue(p: PurchaseOrder): void { this.act(p, () => this.svc.issue(p.id), 'issued to vendor'); }
  acknowledge(p: PurchaseOrder): void { this.act(p, () => this.svc.acknowledge(p.id), 'acknowledged'); }

  beginCancel(p: PurchaseOrder): void { this.cancelId.set(p.id); this.cancelReason = ''; this.grnId.set(null); }
  confirmCancel(p: PurchaseOrder): void {
    if (!this.cancelReason.trim()) { this.error.set('A cancellation reason is required.'); return; }
    this.act(p, () => this.svc.cancel(p.id, this.cancelReason.trim()), 'cancelled', () => this.cancelId.set(null));
  }

  // ── GRN ──

  remaining(l: PoLine): number { return l.orderedQuantity - l.receivedQuantity; }

  beginGrn(p: PurchaseOrder): void {
    this.grnId.set(p.id);
    this.cancelId.set(null);
    this.grnWarehouseId = p.destinationWarehouseId;
    this.grnLines = {};
    const today = new Date().toISOString().slice(0, 10);
    for (const l of p.lines) {
      if (this.remaining(l) <= 0) continue;
      this.grnLines[l.id] = {
        quantity: this.remaining(l),
        batchNumber: '', manufactureDate: today, expiryDate: '', manufacturer: p.vendorName ?? ''
      };
    }
    this.error.set(null); this.notice.set(null);
  }

  confirmGrn(p: PurchaseOrder): void {
    const lines: GrnLineRequest[] = [];
    for (const l of p.lines) {
      const g = this.grnLines[l.id];
      if (!g || !g.quantity || g.quantity <= 0) continue;
      if (g.quantity > this.remaining(l)) { this.error.set(`${l.drugCode}: cannot receive more than the remaining ${this.remaining(l)}.`); return; }
      if (!g.batchNumber.trim()) { this.error.set(`${l.drugCode}: batch number is required.`); return; }
      if (!g.expiryDate || g.expiryDate <= g.manufactureDate) { this.error.set(`${l.drugCode}: expiry must be after manufacture date.`); return; }
      lines.push({
        lineId: l.id, quantity: g.quantity, batchNumber: g.batchNumber.trim(),
        manufactureDate: g.manufactureDate, expiryDate: g.expiryDate,
        manufacturer: g.manufacturer || undefined
      });
    }
    if (lines.length === 0) { this.error.set('Enter a quantity > 0 with batch details for at least one line.'); return; }

    this.act(p, () => this.svc.grn(p.id, { warehouseId: this.grnWarehouseId || undefined, lines }),
      'goods received — stock and ledger updated', () => this.grnId.set(null));
  }

  private act(p: PurchaseOrder, call: () => ReturnType<PurchaseOrdersService['issue']>, verb: string, after?: () => void): void {
    this.busyId.set(p.id);
    this.error.set(null); this.notice.set(null);
    call().pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`PO ${p.poNumber} ${verb}.`); after?.(); this.reload(); },
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
