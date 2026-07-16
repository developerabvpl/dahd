import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ColdChainLog, CreateColdChainLogRequest, Warehouse } from '../../core/models';

@Component({
  selector: 'app-coldchain',
  imports: [CommonModule, FormsModule],
  templateUrl: './coldchain.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColdchainComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly destroy$ = new Subject<void>();

  readonly logs = signal<ColdChainLog[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly loading = signal(true);
  readonly ackingId = signal<string | null>(null);
  readonly ackInput = signal<Record<string, string>>({});
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly saving = signal(false);

  // record-reading form
  readonly showRecord = signal(false);
  reading: CreateColdChainLogRequest = this.blankReading();

  readonly canRecord = () => this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge');

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    forkJoin({
      logs: this.api.getColdChainLogs({ hours: 168 }),
      warehouses: this.api.getWarehouses()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ logs, warehouses }) => {
          this.logs.set(logs);
          this.warehouses.set(warehouses.filter(w => w.coldChainCapable));
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  blankReading(): CreateColdChainLogRequest {
    return {
      warehouseId: '', deviceId: '', deviceName: '',
      readingAt: new Date().toISOString().slice(0, 16),
      temperatureCelsius: 5, remarks: ''
    };
  }

  toggleRecord(): void {
    this.showRecord.update(v => !v);
    if (this.showRecord()) { this.reading = this.blankReading(); this.error.set(null); this.notice.set(null); }
  }

  record(): void {
    const r = this.reading;
    if (!r.warehouseId || !r.deviceId) { this.error.set('Warehouse and device id are required.'); return; }
    this.saving.set(true);
    this.error.set(null); this.notice.set(null);
    this.api.createColdChainLog({ ...r, readingAt: new Date(r.readingAt).toISOString() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: log => {
          this.saving.set(false);
          this.notice.set(log.isBreach
            ? `Reading recorded — ${log.temperatureCelsius} °C is a BREACH; acknowledge it below.`
            : `Reading recorded (${log.temperatureCelsius} °C, in range).`);
          this.showRecord.set(false);
          this.load();
        },
        error: e => {
          this.saving.set(false);
          this.error.set(e?.error?.title ?? (typeof e?.error === 'string' ? e.error : null) ?? e?.message ?? 'Failed');
        }
      });
  }

  canAck(l: ColdChainLog): boolean {
    return l.isBreach
      && !l.acknowledgedAt
      && this.auth.hasRole('Admin', 'Director', 'Cvo', 'WarehouseIncharge');
  }

  setAction(id: string, value: string): void {
    this.ackInput.update(m => ({ ...m, [id]: value }));
  }

  acknowledge(l: ColdChainLog): void {
    const action = (this.ackInput()[l.id] ?? '').trim();
    if (!action) { this.error.set('Corrective action is required.'); return; }
    this.ackingId.set(l.id);
    this.error.set(null);
    this.api.acknowledgeBreach(l.id, { correctiveAction: action })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.ackingId.set(null); this.load(); },
        error: e => {
          this.ackingId.set(null);
          this.error.set(e?.error?.title ?? e?.error ?? e?.message ?? 'Acknowledge failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
