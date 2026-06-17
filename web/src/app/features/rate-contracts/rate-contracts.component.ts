import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { RateContractsService } from '../../core/rate-contracts/rate-contracts.service';
import { CheapestRateRow, RateContract, RateContractCategory } from '../../core/rate-contracts/rate-contracts.models';

@Component({
  selector: 'app-rate-contracts',
  imports: [CommonModule, FormsModule],
  templateUrl: './rate-contracts.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RateContractsComponent implements OnInit, OnDestroy {
  private readonly svc = inject(RateContractsService);
  private readonly destroy$ = new Subject<void>();

  readonly contracts = signal<RateContract[]>([]);
  readonly cheapest = signal<CheapestRateRow[]>([]);
  readonly loading = signal(true);
  readonly categoryFilter = signal<RateContractCategory | ''>('');

  readonly categories: RateContractCategory[] =
    ['Medicines', 'Vaccines', 'Equipment', 'ColdChain', 'LabConsumables', 'AiConsumables', 'Services', 'Other'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    const cat = this.categoryFilter() || undefined;
    forkJoin({
      contracts: this.svc.list({ category: cat || undefined }),
      cheapest: this.svc.cheapest()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ contracts, cheapest }) => {
          this.contracts.set(contracts);
          this.cheapest.set(cheapest);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  expiryCls(days: number): string {
    if (days < 0) return 'bad';
    if (days <= 60) return 'warn';
    return 'ok';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
