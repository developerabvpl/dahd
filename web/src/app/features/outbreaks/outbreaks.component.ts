import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { InaphService } from '../../core/inaph/inaph.service';
import { OutbreakAlert } from '../../core/inaph/inaph.models';

@Component({
  selector: 'app-outbreaks',
  imports: [CommonModule],
  templateUrl: './outbreaks.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OutbreaksComponent implements OnInit, OnDestroy {
  private readonly inaph = inject(InaphService);
  private readonly destroy$ = new Subject<void>();

  readonly alerts = signal<OutbreakAlert[]>([]);
  readonly loading = signal(true);
  readonly days = signal(14);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.inaph.outbreaks(this.days())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: a => { this.alerts.set(a); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  setDays(d: number): void { this.days.set(d); this.load(); }

  cls(s: OutbreakAlert['severity']): string {
    return s === 'Critical' ? 'bad' : s === 'Warning' ? 'warn' : '';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
