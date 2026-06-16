import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Indent } from '../../core/models';

@Component({
  selector: 'app-indents',
  imports: [CommonModule],
  templateUrl: './indents.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IndentsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly indents = signal<Indent[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getIndents()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: i => { this.indents.set(i); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  statusBadge(s: string): string {
    switch (s) {
      case 'Draft': return '';
      case 'Submitted': case 'Approved': case 'Issued': return 'warn';
      case 'Received': case 'Closed': return 'ok';
      case 'Rejected': return 'bad';
      default: return '';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
