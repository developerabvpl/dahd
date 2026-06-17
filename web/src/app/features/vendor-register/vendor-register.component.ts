import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { VendorService } from '../../core/vendor/vendor.service';
import { VENDOR_CATEGORIES, VendorRegistrationRequest } from '../../core/vendor/vendor.models';

@Component({
  selector: 'app-vendor-register',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './vendor-register.component.html',
  styleUrl: './vendor-register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VendorRegisterComponent implements OnDestroy {
  private readonly svc = inject(VendorService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal(false);

  readonly categoryOptions = VENDOR_CATEGORIES;
  readonly selectedCategories = signal<Set<number>>(new Set());

  draft: VendorRegistrationRequest = {
    username: '', password: '',
    legalName: '', tradeName: '',
    contactPerson: '', contactEmail: '', contactPhone: '',
    address: '', city: '', state: 'Uttar Pradesh', pincode: '',
    gstin: '', pan: '', udyamRegNumber: '',
    isManufacturer: false, isMsme: false,
    categories: 0
  };

  toggleCategory(v: number): void {
    this.selectedCategories.update(s => {
      const next = new Set(s);
      next.has(v) ? next.delete(v) : next.add(v);
      return next;
    });
  }

  submit(): void {
    if (this.busy()) return;
    const categories = [...this.selectedCategories()].reduce((a, b) => a | b, 0);
    if (!categories) { this.error.set('Pick at least one category.'); return; }
    this.busy.set(true);
    this.error.set(null);
    this.svc.register({ ...this.draft, categories })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busy.set(false); this.success.set(true); },
        error: e => {
          this.busy.set(false);
          this.error.set(e?.error ?? e?.error?.title ?? e?.message ?? 'Registration failed');
        }
      });
  }

  goLogin(): void { this.router.navigate(['/login']); }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
