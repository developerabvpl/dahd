import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthSession, AuthUser, LoginRequest } from './auth.models';

const STORAGE_KEY = 'dahd.auth.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = environment.apiUrl;

  private readonly _session = signal<AuthSession | null>(this.loadStoredSession());

  readonly session = this._session.asReadonly();
  readonly user = computed<AuthUser | null>(() => this._session()?.user ?? null);
  readonly isAuthenticated = computed(() => !!this._session());

  login(req: LoginRequest): Observable<AuthSession> {
    return this.http.post<AuthSession>(`${this.base}/auth/login`, req).pipe(
      tap(s => this.persistSession(s))
    );
  }

  refresh(): Observable<AuthSession> {
    const current = this._session();
    if (!current) throw new Error('No session to refresh');
    return this.http.post<AuthSession>(`${this.base}/auth/refresh`, {
      refreshToken: current.refreshToken
    }).pipe(tap(s => this.persistSession(s)));
  }

  logout(): void {
    const current = this._session();
    if (current) {
      this.http.post(`${this.base}/auth/logout`, { refreshToken: current.refreshToken })
        .subscribe({ next: () => {}, error: () => {} });
    }
    this.clearSession();
    this.router.navigate(['/login']);
  }

  get accessToken(): string | null {
    return this._session()?.accessToken ?? null;
  }

  hasRole(...roles: string[]): boolean {
    const r = this.user()?.role;
    return !!r && roles.includes(r);
  }

  private persistSession(s: AuthSession): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    this._session.set(s);
  }

  private clearSession(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._session.set(null);
  }

  private loadStoredSession(): AuthSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const s = JSON.parse(raw) as AuthSession;
      if (new Date(s.refreshExpiresAt) <= new Date()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return s;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
