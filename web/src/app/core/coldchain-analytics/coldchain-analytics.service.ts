import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BreachHourMatrixCell, DeviceAnalyticsRow } from './coldchain-analytics.models';

@Injectable({ providedIn: 'root' })
export class ColdChainAnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/coldchain/analytics`;

  byDevice(days = 30, warehouseId?: string): Observable<DeviceAnalyticsRow[]> {
    const params: Record<string, string> = { days: String(days) };
    if (warehouseId) params['warehouseId'] = warehouseId;
    return this.http.get<DeviceAnalyticsRow[]>(`${this.base}/by-device`, { params });
  }

  breachHeatmap(days = 30, warehouseId?: string): Observable<BreachHourMatrixCell[]> {
    const params: Record<string, string> = { days: String(days) };
    if (warehouseId) params['warehouseId'] = warehouseId;
    return this.http.get<BreachHourMatrixCell[]>(`${this.base}/breach-heatmap`, { params });
  }
}
