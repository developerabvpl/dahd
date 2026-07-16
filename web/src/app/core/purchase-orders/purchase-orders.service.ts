import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePoRequest, GrnRequest, PoStatus, PurchaseOrder } from './purchase-orders.models';

@Injectable({ providedIn: 'root' })
export class PurchaseOrdersService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/purchase-orders`;

  list(status?: PoStatus): Observable<PurchaseOrder[]> {
    const params: Record<string, string> = {};
    if (status) params['status'] = status;
    return this.http.get<PurchaseOrder[]>(this.base, { params });
  }

  create(req: CreatePoRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(this.base, req);
  }

  issue(id: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.base}/${id}/issue`, {});
  }

  acknowledge(id: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.base}/${id}/acknowledge`, {});
  }

  cancel(id: string, reason: string): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.base}/${id}/cancel`, { reason });
  }

  grn(id: string, req: GrnRequest): Observable<PurchaseOrder> {
    return this.http.post<PurchaseOrder>(`${this.base}/${id}/grn`, req);
  }
}
