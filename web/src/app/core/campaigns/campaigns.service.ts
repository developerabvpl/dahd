import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CampaignStatus, DraftCampaignIndentsRequest, DraftCampaignIndentsResponse,
  ProcurementCampaign
} from './campaigns.models';

@Injectable({ providedIn: 'root' })
export class CampaignsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/procurement-campaigns`;

  list(status?: CampaignStatus): Observable<ProcurementCampaign[]> {
    const params: Record<string, string> = {};
    if (status) params['status'] = status;
    return this.http.get<ProcurementCampaign[]>(this.base, { params });
  }

  upcoming(take = 5): Observable<ProcurementCampaign[]> {
    return this.http.get<ProcurementCampaign[]>(`${this.base}/upcoming`, { params: { take: String(take) } });
  }

  draftIndents(id: string, req: DraftCampaignIndentsRequest): Observable<DraftCampaignIndentsResponse> {
    return this.http.post<DraftCampaignIndentsResponse>(`${this.base}/${id}/draft-indents`, req);
  }
}
