import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ColdChainLog } from '../../core/models';

@Component({
  selector: 'app-coldchain',
  imports: [CommonModule],
  templateUrl: './coldchain.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColdchainComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly logs = signal<ColdChainLog[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getColdChainLogs({ hours: 168 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: l => { this.logs.set(l); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
