import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Drug, StockLedgerRow, StockMovementType, Warehouse } from '../../core/models';

@Component({
  selector: 'app-stock-ledger',
  imports: [CommonModule, FormsModule],
  templateUrl: './stock-ledger.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StockLedgerComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly rows = signal<StockLedgerRow[]>([]);
  readonly drugs = signal<Drug[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly drugId = signal<string>('');
  readonly warehouseId = signal<string>('');

  ngOnInit(): void {
    forkJoin({ drugs: this.api.getDrugs(), warehouses: this.api.getWarehouses() })
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ drugs, warehouses }) => { this.drugs.set(drugs); this.warehouses.set(warehouses); });
  }

  run(): void {
    if (!this.drugId() && !this.warehouseId()) { this.error.set('Pick a drug and/or a warehouse.'); return; }
    this.loading.set(true); this.error.set(null);
    this.api.getStockLedger({ drugId: this.drugId() || undefined, warehouseId: this.warehouseId() || undefined, take: 500 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => { this.rows.set(r); this.loading.set(false); },
        error: e => { this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Load failed'); this.loading.set(false); }
      });
  }

  typeCls(t: StockMovementType): string {
    switch (t) {
      case 'Receipt': case 'ReceiveIn': case 'Opening': return 'ok';
      case 'IssueOut': case 'Dispense': case 'Wastage': return 'bad';
      default: return '';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
