import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { NetworkService } from '../../core/offline/network.service';
import { OfflineQueueService } from '../../core/offline/offline-queue.service';
import { environment } from '../../../environments/environment';
import { AnimalSpecies, Batch, DispenseEvent, Facility } from '../../core/models';

interface DispensePayload {
  batchId: string;
  quantity: number;
  facilityId: string;
  animalEarTag?: string;
  animalSpecies: AnimalSpecies;
  ownerName?: string;
  ownerMobile?: string;
  diagnosis?: string;
  vetName?: string;
  vetLicenceNo?: string;
  remarks?: string;
}

@Component({
  selector: 'app-dispense',
  imports: [CommonModule, FormsModule],
  templateUrl: './dispense.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DispenseComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly net = inject(NetworkService);
  private readonly queue = inject(OfflineQueueService);
  private readonly destroy$ = new Subject<void>();

  readonly events = signal<DispenseEvent[]>([]);
  readonly batches = signal<Batch[]>([]);
  readonly facilities = signal<Facility[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly notice = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  readonly online = this.net.online;
  readonly pending = this.queue.pendingCount;

  readonly canDispense = computed(() =>
    this.auth.hasRole('Admin', 'FacilityVet', 'MvuVet')
  );

  readonly speciesOptions: AnimalSpecies[] =
    ['Cattle', 'Buffalo', 'Sheep', 'Goat', 'Pig', 'Poultry', 'Equine', 'Other'];

  draft: DispensePayload = this.blankDraft();

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    this.loading.set(true);
    forkJoin({
      events: this.api.getDispenseEvents({ days: 30 }),
      batches: this.api.getBatches(),
      facilities: this.api.getFacilities()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ events, batches, facilities }) => {
          this.events.set(events);
          this.batches.set(batches.filter(b => b.status === 'InStore' && b.quantity > 0));
          this.facilities.set(facilities);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  blankDraft(): DispensePayload {
    return {
      batchId: '',
      quantity: 1,
      facilityId: '',
      animalEarTag: '',
      animalSpecies: 'Cattle',
      ownerName: '',
      ownerMobile: '',
      diagnosis: '',
      vetName: this.auth.user()?.displayName ?? '',
      vetLicenceNo: '',
      remarks: ''
    };
  }

  async submit(): Promise<void> {
    if (this.saving()) return;
    if (!this.draft.batchId || !this.draft.facilityId || this.draft.quantity <= 0) {
      this.error.set('Pick a batch, a facility, and a positive quantity.');
      return;
    }
    this.error.set(null);
    this.notice.set(null);
    this.saving.set(true);

    const payload: DispensePayload = { ...this.draft };

    if (this.net.online()) {
      this.http.post<DispenseEvent>(`${environment.apiUrl}/dispense`, payload)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.saving.set(false);
            this.notice.set('Dispensed and posted.');
            this.draft = this.blankDraft();
            this.refresh();
          },
          error: async e => {
            await this.queue.enqueue(payload);
            this.saving.set(false);
            this.notice.set(`Server unreachable — queued for sync (${e?.status ?? 'err'}).`);
            this.draft = this.blankDraft();
          }
        });
    } else {
      await this.queue.enqueue(payload);
      this.saving.set(false);
      this.notice.set('Offline — saved to local queue; will sync when online.');
      this.draft = this.blankDraft();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
