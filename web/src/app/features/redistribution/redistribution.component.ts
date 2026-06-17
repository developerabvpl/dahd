import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { RedistributionService } from '../../core/redistribution/redistribution.service';
import { RedistributionSuggestion, RedistributionUrgency } from '../../core/redistribution/redistribution.models';

@Component({
  selector: 'app-redistribution',
  imports: [CommonModule, FormsModule],
  templateUrl: './redistribution.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RedistributionComponent implements OnInit, OnDestroy {
  private readonly svc = inject(RedistributionService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly suggestions = signal<RedistributionSuggestion[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly withinDays = signal(90);
  readonly urgencyFilter = signal<RedistributionUrgency | ''>('');

  readonly canCreate = computed(() =>
    this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));

  readonly filtered = computed(() => {
    const f = this.urgencyFilter();
    return f ? this.suggestions().filter(s => s.urgency === f) : this.suggestions();
  });

  readonly urgentCount = computed(() => this.suggestions().filter(s => s.urgency === 'Urgent').length);
  readonly watchCount  = computed(() => this.suggestions().filter(s => s.urgency === 'Watch').length);
  readonly routineCount = computed(() => this.suggestions().filter(s => s.urgency === 'Routine').length);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.svc.suggestions(this.withinDays(), 100)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => { this.suggestions.set(r); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  setWindow(days: number): void {
    this.withinDays.set(days);
    this.load();
  }

  urgencyCls(u: RedistributionUrgency): string {
    return u === 'Urgent' ? 'bad' : u === 'Watch' ? 'warn' : '';
  }

  identityKey(s: RedistributionSuggestion): string {
    return `${s.donorBatchId}|${s.recipientWarehouseId}`;
  }

  create(s: RedistributionSuggestion): void {
    const key = this.identityKey(s);
    this.busyId.set(key);
    this.error.set(null);
    this.notice.set(null);
    this.svc.createIndent({
      donorBatchId: s.donorBatchId,
      recipientWarehouseId: s.recipientWarehouseId,
      quantity: s.suggestedQuantity
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: r => {
          this.busyId.set(null);
          this.notice.set(`Draft indent ${r.indentNumber} created. Open Indents to progress it.`);
          this.load();
        },
        error: e => {
          this.busyId.set(null);
          this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Create failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
