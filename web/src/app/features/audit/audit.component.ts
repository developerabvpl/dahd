import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuditEvent } from '../../core/auth/auth.models';

@Component({
  selector: 'app-audit',
  imports: [CommonModule],
  templateUrl: './audit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuditComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly destroy$ = new Subject<void>();

  readonly events = signal<AuditEvent[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.http.get<AuditEvent[]>(`${environment.apiUrl}/audit/events?days=30&take=200`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: e => { this.events.set(e); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
