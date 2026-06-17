import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ColdChainLog } from '../../core/models';

@Component({
  selector: 'app-coldchain',
  imports: [CommonModule, FormsModule],
  templateUrl: './coldchain.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColdchainComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly logs = signal<ColdChainLog[]>([]);
  readonly loading = signal(true);
  readonly ackingId = signal<string | null>(null);
  readonly ackInput = signal<Record<string, string>>({});
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.api.getColdChainLogs({ hours: 168 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: l => { this.logs.set(l); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  canAck(l: ColdChainLog): boolean {
    return l.isBreach
      && !l.acknowledgedAt
      && this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge');
  }

  setAction(id: string, value: string): void {
    this.ackInput.update(m => ({ ...m, [id]: value }));
  }

  acknowledge(l: ColdChainLog): void {
    const action = (this.ackInput()[l.id] ?? '').trim();
    if (!action) { this.error.set('Corrective action is required.'); return; }
    this.ackingId.set(l.id);
    this.error.set(null);
    this.api.acknowledgeBreach(l.id, { correctiveAction: action })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.ackingId.set(null); this.load(); },
        error: e => {
          this.ackingId.set(null);
          this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Acknowledge failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
