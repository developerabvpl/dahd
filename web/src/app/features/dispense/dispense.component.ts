import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DispenseEvent } from '../../core/models';

@Component({
  selector: 'app-dispense',
  imports: [CommonModule],
  templateUrl: './dispense.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DispenseComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly events = signal<DispenseEvent[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getDispenseEvents({ days: 30 })
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
