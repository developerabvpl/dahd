import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { NetworkService } from '../core/offline/network.service';
import { OfflineQueueService } from '../core/offline/offline-queue.service';

@Component({
  selector: 'app-shell',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly network = inject(NetworkService);
  private readonly queue = inject(OfflineQueueService);

  readonly user = this.auth.user;
  readonly online = this.network.online;
  readonly pending = this.queue.pendingCount;
  readonly draining = this.queue.draining;

  readonly isVendor = () => this.auth.hasRole('Vendor');
  readonly isAhdAdmin = () => this.auth.hasRole('Admin', 'Director');
  readonly isOperator = () => this.auth.hasRole(
    'Admin', 'Director', 'Cvo', 'WarehouseIncharge', 'FacilityVet', 'MvuVet', 'Readonly'
  );

  logout(): void {
    this.auth.logout();
  }

  drainNow(): void {
    this.queue.drain().catch(() => {});
  }
}
