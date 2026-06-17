export type AppRole =
  | 'Admin' | 'Director' | 'Cvo'
  | 'WarehouseIncharge' | 'FacilityVet' | 'MvuVet' | 'Readonly' | 'Vendor';

export interface AuthUser {
  id: string;
  username: string;
  displayName: string;
  email?: string;
  role: AppRole;
  warehouseId?: string;
  facilityId?: string;
}

export interface AuthSession {
  accessToken: string;
  accessExpiresAt: string;
  refreshToken: string;
  refreshExpiresAt: string;
  user: AuthUser;
}

export interface LoginRequest { username: string; password: string; }
export interface RefreshRequest { refreshToken: string; }

export interface AuditEvent {
  id: string;
  occurredAt: string;
  entityType: string;
  entityId: string;
  action: string;
  actorUserId?: string;
  actorUsername?: string;
  actorRole?: string;
  ipAddress?: string;
  correlationId?: string;
  summary?: string;
  beforeJson?: string;
  afterJson?: string;
}
