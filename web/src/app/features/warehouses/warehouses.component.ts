import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Warehouse } from '../../core/models';

@Component({
  selector: 'app-warehouses',
  imports: [CommonModule],
  templateUrl: './warehouses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WarehousesComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly destroy$ = new Subject<void>();

  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getWarehouses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: w => { this.warehouses.set(w); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
