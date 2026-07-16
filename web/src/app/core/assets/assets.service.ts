import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Asset, AssetCategory, AssetJob, AssetKpi, AssetStatus,
  CompleteJobRequest, CreateAmcRequest, CreateAssetRequest,
  CreateScheduleRequest, LogBreakdownRequest,
  MaintenanceDueRow, MaintenanceJobStatus
} from './assets.models';

@Injectable({ providedIn: 'root' })
export class AssetsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  list(opts?: { status?: AssetStatus; category?: AssetCategory }): Observable<Asset[]> {
    const params: Record<string, string> = {};
    if (opts?.status) params['status'] = opts.status;
    if (opts?.category) params['category'] = opts.category;
    return this.http.get<Asset[]>(`${this.base}/assets`, { params });
  }

  get(id: string): Observable<Asset> {
    return this.http.get<Asset>(`${this.base}/assets/${id}`);
  }

  kpis(): Observable<AssetKpi> {
    return this.http.get<AssetKpi>(`${this.base}/assets/kpis`);
  }

  create(req: CreateAssetRequest): Observable<Asset> {
    return this.http.post<Asset>(`${this.base}/assets`, req);
  }

  addAmc(id: string, req: CreateAmcRequest): Observable<Asset> {
    return this.http.post<Asset>(`${this.base}/assets/${id}/amc`, req);
  }

  updateStatus(id: string, status: AssetStatus, condition?: string, notes?: string): Observable<Asset> {
    return this.http.patch<Asset>(`${this.base}/assets/${id}/status`, { status, condition, notes });
  }

  addSchedule(id: string, req: CreateScheduleRequest): Observable<Asset> {
    return this.http.post<Asset>(`${this.base}/assets/${id}/schedules`, req);
  }

  // ---- maintenance ----

  due(withinDays = 30): Observable<MaintenanceDueRow[]> {
    return this.http.get<MaintenanceDueRow[]>(`${this.base}/maintenance/due`, { params: { withinDays: String(withinDays) } });
  }

  jobs(opts?: { status?: MaintenanceJobStatus }): Observable<AssetJob[]> {
    const params: Record<string, string> = {};
    if (opts?.status) params['status'] = opts.status;
    return this.http.get<AssetJob[]>(`${this.base}/maintenance/jobs`, { params });
  }

  logBreakdown(assetId: string, req: LogBreakdownRequest): Observable<AssetJob> {
    return this.http.post<AssetJob>(`${this.base}/maintenance/assets/${assetId}/breakdown`, req);
  }

  raisePpmJob(assetId: string, scheduleId: string | undefined, description: string, assignedTo?: string): Observable<AssetJob> {
    return this.http.post<AssetJob>(`${this.base}/maintenance/assets/${assetId}/ppm-job`, { scheduleId, description, assignedTo });
  }

  startJob(jobId: string): Observable<AssetJob> {
    return this.http.post<AssetJob>(`${this.base}/maintenance/jobs/${jobId}/start`, {});
  }

  completeJob(jobId: string, req: CompleteJobRequest): Observable<AssetJob> {
    return this.http.post<AssetJob>(`${this.base}/maintenance/jobs/${jobId}/complete`, req);
  }
}
