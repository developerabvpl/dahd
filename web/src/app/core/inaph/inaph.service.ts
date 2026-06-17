import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { InaphAnimal, OutbreakAlert } from './inaph.models';

@Injectable({ providedIn: 'root' })
export class InaphService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/inaph`;

  lookup(earTag: string): Observable<InaphAnimal> {
    return this.http.get<InaphAnimal>(`${this.base}/lookup/${encodeURIComponent(earTag)}`);
  }

  outbreaks(days = 14): Observable<OutbreakAlert[]> {
    return this.http.get<OutbreakAlert[]>(`${this.base}/outbreak-alerts`, { params: { days: String(days) } });
  }
}
