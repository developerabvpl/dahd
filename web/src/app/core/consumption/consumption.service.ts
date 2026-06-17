import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ConsumptionForecastRow, DraftQuarterlyIndentRequest, DraftQuarterlyIndentResponse
} from './consumption.models';

@Injectable({ providedIn: 'root' })
export class ConsumptionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/consumption`;

  forecast(opts?: {
    warehouseId?: string;
    lookbackDays?: number;
    forecastDays?: number;
    safetyMultiplier?: number;
  }): Observable<ConsumptionForecastRow[]> {
    const params: Record<string, string> = {};
    if (opts?.warehouseId) params['warehouseId'] = opts.warehouseId;
    if (opts?.lookbackDays != null) params['lookbackDays'] = String(opts.lookbackDays);
    if (opts?.forecastDays != null) params['forecastDays'] = String(opts.forecastDays);
    if (opts?.safetyMultiplier != null) params['safetyMultiplier'] = String(opts.safetyMultiplier);
    return this.http.get<ConsumptionForecastRow[]>(`${this.base}/forecast`, { params });
  }

  draftQuarterly(req: DraftQuarterlyIndentRequest): Observable<DraftQuarterlyIndentResponse> {
    return this.http.post<DraftQuarterlyIndentResponse>(`${this.base}/draft-quarterly-indent`, req);
  }
}
