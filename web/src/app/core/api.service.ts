import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Batch, ColdChainLog, DashboardKpi, DispenseEvent,
  Drug, Facility, Indent, Warehouse
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getKpis(): Observable<DashboardKpi> {
    return this.http.get<DashboardKpi>(`${this.base}/dashboard/kpis`);
  }

  getDrugs(opts?: { vaccinesOnly?: boolean; coldChainOnly?: boolean }): Observable<Drug[]> {
    const params: Record<string, string> = {};
    if (opts?.vaccinesOnly) params['vaccinesOnly'] = 'true';
    if (opts?.coldChainOnly) params['coldChainOnly'] = 'true';
    return this.http.get<Drug[]>(`${this.base}/drugs`, { params });
  }

  getWarehouses(): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(`${this.base}/warehouses`);
  }

  getFacilities(): Observable<Facility[]> {
    return this.http.get<Facility[]>(`${this.base}/facilities`);
  }

  getBatches(opts?: {
    warehouseId?: string;
    drugId?: string;
    expiringWithinDays?: number;
  }): Observable<Batch[]> {
    const params: Record<string, string> = {};
    if (opts?.warehouseId) params['warehouseId'] = opts.warehouseId;
    if (opts?.drugId) params['drugId'] = opts.drugId;
    if (opts?.expiringWithinDays != null) params['expiringWithinDays'] = String(opts.expiringWithinDays);
    return this.http.get<Batch[]>(`${this.base}/batches`, { params });
  }

  getIndents(): Observable<Indent[]> {
    return this.http.get<Indent[]>(`${this.base}/indents`);
  }

  getColdChainLogs(opts?: { warehouseId?: string; breachesOnly?: boolean; hours?: number }): Observable<ColdChainLog[]> {
    const params: Record<string, string> = {};
    if (opts?.warehouseId) params['warehouseId'] = opts.warehouseId;
    if (opts?.breachesOnly) params['breachesOnly'] = 'true';
    if (opts?.hours) params['hours'] = String(opts.hours);
    return this.http.get<ColdChainLog[]>(`${this.base}/coldchain/logs`, { params });
  }

  getDispenseEvents(opts?: { facilityId?: string; days?: number }): Observable<DispenseEvent[]> {
    const params: Record<string, string> = {};
    if (opts?.facilityId) params['facilityId'] = opts.facilityId;
    if (opts?.days) params['days'] = String(opts.days);
    return this.http.get<DispenseEvent[]>(`${this.base}/dispense`, { params });
  }
}
