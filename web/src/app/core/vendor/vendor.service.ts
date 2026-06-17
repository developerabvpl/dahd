import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UploadVendorDocumentRequest, Vendor, VendorDocument,
  VendorRegistrationRequest, VendorReviewActionRequest, VendorStatus
} from './vendor.models';

@Injectable({ providedIn: 'root' })
export class VendorService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/vendors`;

  register(req: VendorRegistrationRequest): Observable<Vendor> {
    return this.http.post<Vendor>(`${this.base}/register`, req);
  }

  me(): Observable<Vendor> {
    return this.http.get<Vendor>(`${this.base}/me`);
  }

  uploadMyDoc(req: UploadVendorDocumentRequest): Observable<VendorDocument> {
    return this.http.post<VendorDocument>(`${this.base}/me/documents`, req);
  }

  submitMine(): Observable<Vendor> {
    return this.http.post<Vendor>(`${this.base}/me/submit`, {});
  }

  list(status?: VendorStatus): Observable<Vendor[]> {
    const params: Record<string, string> = {};
    if (status) params['status'] = status;
    return this.http.get<Vendor[]>(this.base, { params });
  }

  get(id: string): Observable<Vendor> {
    return this.http.get<Vendor>(`${this.base}/${id}`);
  }

  startReview(id: string, req: VendorReviewActionRequest = {}): Observable<Vendor> {
    return this.http.post<Vendor>(`${this.base}/${id}/start-review`, req);
  }
  approve(id: string, req: VendorReviewActionRequest = {}): Observable<Vendor> {
    return this.http.post<Vendor>(`${this.base}/${id}/approve`, req);
  }
  reject(id: string, req: VendorReviewActionRequest): Observable<Vendor> {
    return this.http.post<Vendor>(`${this.base}/${id}/reject`, req);
  }
  blacklist(id: string, req: VendorReviewActionRequest): Observable<Vendor> {
    return this.http.post<Vendor>(`${this.base}/${id}/blacklist`, req);
  }
}
