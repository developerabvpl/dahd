import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Batch } from '../../core/models';

@Component({
  selector: 'app-batches',
  imports: [CommonModule],
  templateUrl: './batches.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BatchesComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly batches = signal<Batch[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getBatches()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: b => { this.batches.set(b); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  expiryBadge(days: number): { cls: string; text: string } {
    if (days < 0) return { cls: 'bad', text: 'Expired' };
    if (days <= 30) return { cls: 'warn', text: `${days}d` };
    if (days <= 90) return { cls: '', text: `${days}d` };
    return { cls: 'ok', text: `${days}d` };
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
