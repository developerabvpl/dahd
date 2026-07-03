import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ParAutoIndentRequest, ParAutoIndentResponse, ParLevelRow, UpsertParLevelRequest
} from './par-levels.models';

@Injectable({ providedIn: 'root' })
export class ParLevelsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/par-levels`;

  list(opts?: { warehouseId?: string; belowParOnly?: boolean }): Observable<ParLevelRow[]> {
    const params: Record<string, string> = {};
    if (opts?.warehouseId) params['warehouseId'] = opts.warehouseId;
    if (opts?.belowParOnly) params['belowParOnly'] = 'true';
    return this.http.get<ParLevelRow[]>(this.base, { params });
  }

  upsert(req: UpsertParLevelRequest): Observable<ParLevelRow> {
    return this.http.post<ParLevelRow>(this.base, req);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  autoIndent(req: ParAutoIndentRequest): Observable<ParAutoIndentResponse> {
    return this.http.post<ParAutoIndentResponse>(`${this.base}/auto-indent`, req);
  }
}
