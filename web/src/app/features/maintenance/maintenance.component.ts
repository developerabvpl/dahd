import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { AssetsService } from '../../core/assets/assets.service';
import { AssetJob, CalibrationDueRow, IncidentPriority, MaintenanceDueRow, MaintenanceJobStatus } from '../../core/assets/assets.models';

@Component({
  selector: 'app-maintenance',
  imports: [CommonModule, FormsModule],
  templateUrl: './maintenance.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MaintenanceComponent implements OnInit, OnDestroy {
  private readonly svc = inject(AssetsService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly due = signal<MaintenanceDueRow[]>([]);
  readonly calibration = signal<CalibrationDueRow[]>([]);
  readonly jobs = signal<AssetJob[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly withinDays = signal(30);
  readonly jobStatusFilter = signal<MaintenanceJobStatus | ''>('Open');
  readonly resolutionInput = signal<Record<string, string>>({});
  readonly costInput = signal<Record<string, number>>({});

  readonly canAct = computed(() => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge'));

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    forkJoin({
      due: this.svc.due(this.withinDays()),
      calibration: this.svc.calibrationDue(60),
      jobs: this.svc.jobs({ status: this.jobStatusFilter() || undefined })
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ due, calibration, jobs }) => { this.due.set(due); this.calibration.set(calibration); this.jobs.set(jobs); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  setResolution(id: string, v: string): void { this.resolutionInput.update(m => ({ ...m, [id]: v })); }
  setCost(id: string, v: number): void { this.costInput.update(m => ({ ...m, [id]: v })); }

  dueCls(days: number): string {
    if (days < 0) return 'bad';
    if (days <= 15) return 'warn';
    return '';
  }

  jobStatusCls(s: MaintenanceJobStatus): string {
    if (s === 'Completed') return 'ok';
    if (s === 'Open') return 'bad';
    if (s === 'InProgress') return 'warn';
    return '';
  }

  priorityCls(p?: IncidentPriority): string {
    if (p === 'Critical' || p === 'High') return 'bad';
    if (p === 'Medium') return 'warn';
    return 'ok';
  }

  start(j: AssetJob): void {
    this.busyId.set(j.id);
    this.error.set(null); this.notice.set(null);
    this.svc.startJob(j.id).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`${j.jobNumber} started.`); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(e?.error?.title ?? e?.message ?? 'Failed'); }
      });
  }

  complete(j: AssetJob): void {
    const resolution = (this.resolutionInput()[j.id] ?? '').trim();
    if (!resolution) { this.error.set('Enter a resolution to complete the job.'); return; }
    const cost = Number(this.costInput()[j.id] ?? 0) || undefined;
    this.busyId.set(j.id);
    this.error.set(null); this.notice.set(null);
    this.svc.completeJob(j.id, { resolution, cost }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.busyId.set(null); this.notice.set(`${j.jobNumber} completed; asset back to Active and PPM rolled forward.`); this.load(); },
        error: e => { this.busyId.set(null); this.error.set(e?.error?.title ?? e?.message ?? 'Failed'); }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
