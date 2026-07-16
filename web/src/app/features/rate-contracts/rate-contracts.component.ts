import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { RateContractsService } from '../../core/rate-contracts/rate-contracts.service';
import {
  AddRateContractItemRequest, CheapestRateRow, CreateRateContractRequest,
  RateContract, RateContractCategory
} from '../../core/rate-contracts/rate-contracts.models';
import { Drug } from '../../core/models';

@Component({
  selector: 'app-rate-contracts',
  imports: [CommonModule, FormsModule],
  templateUrl: './rate-contracts.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RateContractsComponent implements OnInit, OnDestroy {
  private readonly svc = inject(RateContractsService);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly contracts = signal<RateContract[]>([]);
  readonly cheapest = signal<CheapestRateRow[]>([]);
  readonly drugs = signal<Drug[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly categoryFilter = signal<RateContractCategory | ''>('');

  // create-contract form
  readonly showCreate = signal(false);
  draft: CreateRateContractRequest = this.blankContract();

  // per-contract add-item form
  readonly addItemId = signal<string | null>(null);
  itemDraft: AddRateContractItemRequest = this.blankItem();

  readonly canManage = computed(() => this.auth.hasRole('Admin', 'Director'));

  readonly categories: RateContractCategory[] =
    ['Medicines', 'Vaccines', 'Equipment', 'ColdChain', 'LabConsumables', 'AiConsumables', 'Services', 'Other'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    const cat = this.categoryFilter() || undefined;
    forkJoin({
      contracts: this.svc.list({ category: cat || undefined }),
      cheapest: this.svc.cheapest(),
      drugs: this.api.getDrugs()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ contracts, cheapest, drugs }) => {
          this.contracts.set(contracts);
          this.cheapest.set(cheapest);
          this.drugs.set(drugs);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  blankContract(): CreateRateContractRequest {
    const today = new Date().toISOString().slice(0, 10);
    const nextYear = new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10);
    return {
      contractNumber: '', title: '', category: 'Medicines', leadBody: 'AHD',
      validFrom: today, validUntil: nextYear, sourceUrl: '', notes: ''
    };
  }

  blankItem(): AddRateContractItemRequest {
    return { drugId: '', vendorName: '', unitRate: 0, packSize: '', minOrderQuantity: undefined, remarks: '' };
  }

  toggleCreate(): void {
    this.showCreate.update(v => !v);
    if (this.showCreate()) { this.draft = this.blankContract(); this.error.set(null); this.notice.set(null); }
  }

  createContract(): void {
    const d = this.draft;
    if (!d.contractNumber || !d.title) { this.error.set('Contract number and title are required.'); return; }
    if (d.validUntil < d.validFrom) { this.error.set('Valid-until must be after valid-from.'); return; }
    this.busy.set(true);
    this.error.set(null); this.notice.set(null);
    this.svc.create(d).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: rc => { this.busy.set(false); this.notice.set(`Rate contract ${rc.contractNumber} created. Now add line items to it.`); this.showCreate.set(false); this.load(); },
        error: e => { this.busy.set(false); this.error.set(this.msg(e)); }
      });
  }

  openAddItem(rc: RateContract): void {
    this.addItemId.set(rc.id);
    this.itemDraft = this.blankItem();
    this.error.set(null); this.notice.set(null);
  }

  closeAddItem(): void { this.addItemId.set(null); }

  saveItem(rc: RateContract): void {
    const d = this.itemDraft;
    if (!d.drugId) { this.error.set('Pick a drug for the line item.'); return; }
    if (d.unitRate <= 0) { this.error.set('Unit rate must be positive.'); return; }
    this.busy.set(true);
    this.error.set(null); this.notice.set(null);
    this.svc.addItem(rc.id, { ...d, vendorName: d.vendorName || undefined })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busy.set(false); this.notice.set(`Item added to ${rc.contractNumber}.`); this.closeAddItem(); this.load(); },
        error: e => { this.busy.set(false); this.error.set(this.msg(e)); }
      });
  }

  expiryCls(days: number): string {
    if (days < 0) return 'bad';
    if (days <= 60) return 'warn';
    return 'ok';
  }

  private msg(e: any): string {
    return e?.error?.title ?? (typeof e?.error === 'string' ? e.error : null) ?? e?.message ?? 'Action failed';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
