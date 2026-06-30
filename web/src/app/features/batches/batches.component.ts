import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Batch, CreateBatchRequest, Drug, Warehouse } from '../../core/models';

@Component({
  selector: 'app-batches',
  imports: [CommonModule, FormsModule],
  templateUrl: './batches.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BatchesComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly batches = signal<Batch[]>([]);
  readonly drugs = signal<Drug[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly showReceive = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly canReceive = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));

  draft: CreateBatchRequest = this.blankDraft();

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    forkJoin({
      batches: this.api.getBatches(),
      drugs: this.api.getDrugs(),
      warehouses: this.api.getWarehouses()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ batches, drugs, warehouses }) => {
          this.batches.set(batches);
          this.drugs.set(drugs);
          this.warehouses.set(warehouses);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  blankDraft(): CreateBatchRequest {
    const today = new Date().toISOString().slice(0, 10);
    return {
      drugId: '', batchNumber: '', manufactureDate: today, expiryDate: '',
      manufacturer: '', quantity: 0, unitCost: 0, currentWarehouseId: '', purchaseOrderRef: ''
    };
  }

  toggleReceive(): void {
    this.showReceive.update(v => !v);
    if (this.showReceive()) { this.draft = this.blankDraft(); this.error.set(null); this.notice.set(null); }
  }

  expiryBadge(days: number): { cls: string; text: string } {
    if (days < 0) return { cls: 'bad', text: 'Expired' };
    if (days <= 30) return { cls: 'warn', text: `${days}d` };
    if (days <= 90) return { cls: '', text: `${days}d` };
    return { cls: 'ok', text: `${days}d` };
  }

  receive(): void {
    const d = this.draft;
    if (!d.drugId || !d.currentWarehouseId || !d.batchNumber) { this.error.set('Drug, warehouse and batch number are required.'); return; }
    if (!d.expiryDate) { this.error.set('Expiry date is required.'); return; }
    if (d.expiryDate <= d.manufactureDate) { this.error.set('Expiry must be after manufacture date.'); return; }
    if (d.quantity <= 0) { this.error.set('Quantity must be greater than zero.'); return; }

    this.saving.set(true);
    this.error.set(null); this.notice.set(null);
    this.api.createBatch(d).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: b => {
          this.saving.set(false);
          this.notice.set(`Received batch ${b.batchNumber} of ${b.drugName} (${b.quantity}) into ${b.warehouseName}.`);
          this.showReceive.set(false);
          this.load();
        },
        error: e => {
          this.saving.set(false);
          this.error.set(e?.error?.title ?? (typeof e?.error === 'string' ? e.error : null) ?? e?.message ?? 'Goods receipt failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
