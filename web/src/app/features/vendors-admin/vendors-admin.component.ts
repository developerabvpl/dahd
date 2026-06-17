import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { VendorService } from '../../core/vendor/vendor.service';
import { VENDOR_CATEGORIES, Vendor, VendorStatus } from '../../core/vendor/vendor.models';

@Component({
  selector: 'app-vendors-admin',
  imports: [CommonModule, FormsModule],
  templateUrl: './vendors-admin.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VendorsAdminComponent implements OnInit, OnDestroy {
  private readonly svc = inject(VendorService);
  private readonly destroy$ = new Subject<void>();

  readonly vendors = signal<Vendor[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly statusFilter = signal<VendorStatus | ''>('');
  readonly notice = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly remarks = signal<Record<string, string>>({});
  readonly inspectionDate = signal<Record<string, string>>({});

  readonly statuses: VendorStatus[] =
    ['Draft', 'Submitted', 'UnderReview', 'Approved', 'Rejected', 'Blacklisted'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.svc.list(this.statusFilter() || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: v => { this.vendors.set(v); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  setFilter(s: VendorStatus | ''): void {
    this.statusFilter.set(s);
    this.load();
  }

  setRemarks(id: string, v: string): void { this.remarks.update(m => ({ ...m, [id]: v })); }
  setInspection(id: string, v: string): void { this.inspectionDate.update(m => ({ ...m, [id]: v })); }

  categoryLabels(mask: number): string {
    return VENDOR_CATEGORIES.filter(c => (mask & c.value) !== 0).map(c => c.label).join(', ') || '—';
  }

  statusBadge(s: VendorStatus): string {
    if (s === 'Approved') return 'ok';
    if (s === 'Rejected' || s === 'Blacklisted') return 'bad';
    if (s === 'Submitted' || s === 'UnderReview') return 'warn';
    return '';
  }

  startReview(v: Vendor): void { this.act(v, 'startReview'); }
  approve(v: Vendor): void { this.act(v, 'approve'); }
  reject(v: Vendor): void {
    if (!(this.remarks()[v.id] ?? '').trim()) { this.error.set('Rejection remarks are required.'); return; }
    this.act(v, 'reject');
  }
  blacklist(v: Vendor): void {
    if (!(this.remarks()[v.id] ?? '').trim()) { this.error.set('Blacklist reason is required.'); return; }
    this.act(v, 'blacklist');
  }

  private act(v: Vendor, kind: 'startReview' | 'approve' | 'reject' | 'blacklist'): void {
    this.busyId.set(v.id);
    this.error.set(null); this.notice.set(null);
    const remarks = (this.remarks()[v.id] ?? '').trim() || undefined;
    const inspection = (this.inspectionDate()[v.id] ?? '').trim() || undefined;
    const call$ =
      kind === 'startReview' ? this.svc.startReview(v.id, { remarks, scheduledInspectionAt: inspection }) :
      kind === 'approve'     ? this.svc.approve(v.id, { remarks }) :
      kind === 'reject'      ? this.svc.reject(v.id, { remarks }) :
                               this.svc.blacklist(v.id, { blacklistReason: remarks });
    call$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`${kind} done`); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(e?.error ?? e?.message ?? 'Action failed'); }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
