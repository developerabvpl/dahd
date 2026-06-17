import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy$ = new Subject<void>();

  username = '';
  password = '';
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly demoUsers = [
    { user: 'admin',    pass: 'admin123',    role: 'Admin' },
    { user: 'director', pass: 'director123', role: 'Director' },
    { user: 'cvo',      pass: 'cvo123',      role: 'CVO' },
    { user: 'wh',       pass: 'wh123',       role: 'Warehouse In-Charge' },
    { user: 'vet',      pass: 'vet123',      role: 'Facility Vet' },
    { user: 'mvuvet',   pass: 'mvu123',      role: 'MVU Vet' },
    { user: 'vendor1',  pass: 'vendor123',   role: 'Vendor' }
  ];

  fill(user: string, pass: string): void {
    this.username = user;
    this.password = pass;
  }

  submit(): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.auth.login({ username: this.username, password: this.password })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.busy.set(false);
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
          this.router.navigateByUrl(returnUrl);
        },
        error: e => {
          this.busy.set(false);
          this.error.set(e?.error?.error ?? 'Login failed');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
