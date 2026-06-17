import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Indent, IndentStatus } from '../../core/models';

type Action = 'submit' | 'approve' | 'issue' | 'receive';

@Component({
  selector: 'app-indents',
  imports: [CommonModule],
  templateUrl: './indents.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IndentsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly indents = signal<Indent[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getIndents()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: i => { this.indents.set(i); this.loading.set(false); },
        error: e => { this.error.set(e?.error?.title ?? e?.message ?? 'Load failed'); this.loading.set(false); }
      });
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

  canDo(action: Action, i: Indent): boolean {
    switch (action) {
      case 'submit':  return i.status === 'Draft'     && this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge');
      case 'approve': return i.status === 'Submitted' && this.auth.hasRole('Admin', 'Director', 'Cvo');
      case 'issue':   return i.status === 'Approved'  && this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge');
      case 'receive': return i.status === 'Issued'    && this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge');
    }
  }

  fire(action: Action, i: Indent): void {
    if (this.busyId() === i.id) return;
    this.busyId.set(i.id);
    this.error.set(null);
    const call$ =
      action === 'submit'  ? this.api.submitIndent(i.id)  :
      action === 'approve' ? this.api.approveIndent(i.id) :
      action === 'issue'   ? this.api.issueIndent(i.id)   :
                             this.api.receiveIndent(i.id);
    call$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.load(); },
        error: e => {
          this.busyId.set(null);
          this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Action failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
