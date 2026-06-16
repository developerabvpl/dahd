import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Drug } from '../../core/models';

@Component({
  selector: 'app-drugs',
  imports: [CommonModule],
  templateUrl: './drugs.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DrugsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly drugs = signal<Drug[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getDrugs()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: d => { this.drugs.set(d); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  trackById(_: number, d: Drug): string { return d.id; }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
