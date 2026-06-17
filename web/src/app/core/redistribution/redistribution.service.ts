import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateRedistributionIndentRequest, CreateRedistributionIndentResponse,
  RedistributionSuggestion
} from './redistribution.models';

@Injectable({ providedIn: 'root' })
export class RedistributionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/redistribution`;

  suggestions(withinDays = 90, maxSuggestions = 50): Observable<RedistributionSuggestion[]> {
    return this.http.get<RedistributionSuggestion[]>(`${this.base}/suggestions`, {
      params: { withinDays: String(withinDays), maxSuggestions: String(maxSuggestions) }
    });
  }

  createIndent(req: CreateRedistributionIndentRequest): Observable<CreateRedistributionIndentResponse> {
    return this.http.post<CreateRedistributionIndentResponse>(`${this.base}/create-indent`, req);
  }
}
