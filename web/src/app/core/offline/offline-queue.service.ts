import { Injectable, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { IDBPDatabase, openDB } from 'idb';
import { environment } from '../../../environments/environment';
import { NetworkService } from './network.service';

const DB_NAME = 'dahd-offline';
const DB_VERSION = 1;
const STORE = 'dispense-queue';

interface QueuedDispense {
  id: number;
  createdAt: string;
  payload: unknown;
  lastError?: string;
}

@Injectable({ providedIn: 'root' })
export class OfflineQueueService {
  private readonly http = inject(HttpClient);
  private readonly network = inject(NetworkService);
  private dbPromise: Promise<IDBPDatabase> | null = null;

  readonly pendingCount = signal(0);
  readonly draining = signal(false);
  readonly lastDrainAt = signal<string | null>(null);
  readonly lastError = signal<string | null>(null);

  constructor() {
    this.refreshCount();
    effect(() => {
      if (this.network.online()) {
        this.drain().catch(() => {});
      }
    });
  }

  private db(): Promise<IDBPDatabase> {
    if (!this.dbPromise) {
      this.dbPromise = openDB(DB_NAME, DB_VERSION, {
        upgrade(db) {
          if (!db.objectStoreNames.contains(STORE)) {
            db.createObjectStore(STORE, { keyPath: 'id', autoIncrement: true });
          }
        }
      });
    }
    return this.dbPromise;
  }

  async enqueue(payload: unknown): Promise<void> {
    const db = await this.db();
    await db.add(STORE, { createdAt: new Date().toISOString(), payload });
    await this.refreshCount();
  }

  async refreshCount(): Promise<void> {
    try {
      const db = await this.db();
      this.pendingCount.set(await db.count(STORE));
    } catch {
      this.pendingCount.set(0);
    }
  }

  async drain(): Promise<void> {
    if (this.draining()) return;
    if (!this.network.online()) return;
    this.draining.set(true);
    try {
      const db = await this.db();
      const items: QueuedDispense[] = await db.getAll(STORE);
      for (const item of items) {
        try {
          await this.http.post(`${environment.apiUrl}/dispense`, item.payload).toPromise();
          await db.delete(STORE, item.id);
        } catch (e: unknown) {
          const msg = (e as { message?: string })?.message ?? 'Drain failed';
          this.lastError.set(msg);
          break;
        }
      }
      this.lastDrainAt.set(new Date().toISOString());
      await this.refreshCount();
    } finally {
      this.draining.set(false);
    }
  }
}
