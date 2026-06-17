import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { StockByDrugRow } from '../../core/models';

@Component({
  selector: 'app-stock',
  imports: [CommonModule],
  templateUrl: './stock.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StockComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly rows = signal<StockByDrugRow[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getStockByDrug()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => { this.rows.set(r); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
