import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { VendorService } from '../../core/vendor/vendor.service';
import {
  UploadVendorDocumentRequest, VENDOR_CATEGORIES, VENDOR_DOCUMENT_TYPES,
  Vendor, VendorDocumentType
} from '../../core/vendor/vendor.models';

@Component({
  selector: 'app-vendor-portal',
  imports: [CommonModule, FormsModule],
  templateUrl: './vendor-portal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VendorPortalComponent implements OnInit, OnDestroy {
  private readonly svc = inject(VendorService);
  private readonly destroy$ = new Subject<void>();

  readonly vendor = signal<Vendor | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly notice = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly docTypes = VENDOR_DOCUMENT_TYPES;

  draftDoc: UploadVendorDocumentRequest = this.blankDoc();

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.svc.me()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: v => { this.vendor.set(v); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  blankDoc(): UploadVendorDocumentRequest {
    return {
      documentType: 'DrugLicence' as VendorDocumentType,
      fileName: '', storageRef: '',
      issuingAuthority: '', certificateNumber: '',
      issuedDate: undefined, expiryDate: undefined, notes: ''
    };
  }

  categoryLabels(mask: number): string {
    return VENDOR_CATEGORIES.filter(c => (mask & c.value) !== 0).map(c => c.label).join(', ') || '—';
  }

  uploadDoc(): void {
    if (!this.draftDoc.fileName) { this.error.set('Provide a file name.'); return; }
    this.busy.set(true); this.error.set(null); this.notice.set(null);
    this.svc.uploadMyDoc(this.draftDoc)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busy.set(false); this.notice.set('Document recorded.'); this.draftDoc = this.blankDoc(); this.load(); },
        error: e => { this.busy.set(false); this.error.set(e?.error ?? e?.message ?? 'Upload failed'); }
      });
  }

  submit(): void {
    this.busy.set(true); this.error.set(null); this.notice.set(null);
    this.svc.submitMine()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busy.set(false); this.notice.set('Submitted for AHD review.'); this.load(); },
        error: e => { this.busy.set(false); this.error.set(e?.error ?? e?.message ?? 'Submit failed'); }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
