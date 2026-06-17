import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CheapestRateRow, RateContract, RateContractCategory, RateContractStatus } from './rate-contracts.models';

@Injectable({ providedIn: 'root' })
export class RateContractsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/rate-contracts`;

  list(opts?: { category?: RateContractCategory; status?: RateContractStatus }): Observable<RateContract[]> {
    const params: Record<string, string> = {};
    if (opts?.category) params['category'] = opts.category;
    if (opts?.status) params['status'] = opts.status;
    return this.http.get<RateContract[]>(this.base, { params });
  }

  get(id: string): Observable<RateContract> {
    return this.http.get<RateContract>(`${this.base}/${id}`);
  }

  cheapest(drugId?: string): Observable<CheapestRateRow[]> {
    const params: Record<string, string> = {};
    if (drugId) params['drugId'] = drugId;
    return this.http.get<CheapestRateRow[]>(`${this.base}/cheapest`, { params });
  }
}
