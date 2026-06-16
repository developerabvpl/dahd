import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Facility } from '../../core/models';

@Component({
  selector: 'app-facilities',
  imports: [CommonModule],
  templateUrl: './facilities.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FacilitiesComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly facilities = signal<Facility[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getFacilities()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: f => { this.facilities.set(f); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
