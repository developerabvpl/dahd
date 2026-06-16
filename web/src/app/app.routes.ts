import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () => import('./shell/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard',   loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'drugs',       loadComponent: () => import('./features/drugs/drugs.component').then(m => m.DrugsComponent) },
      { path: 'warehouses',  loadComponent: () => import('./features/warehouses/warehouses.component').then(m => m.WarehousesComponent) },
      { path: 'facilities',  loadComponent: () => import('./features/facilities/facilities.component').then(m => m.FacilitiesComponent) },
      { path: 'batches',     loadComponent: () => import('./features/batches/batches.component').then(m => m.BatchesComponent) },
      { path: 'indents',     loadComponent: () => import('./features/indents/indents.component').then(m => m.IndentsComponent) },
      { path: 'coldchain',   loadComponent: () => import('./features/coldchain/coldchain.component').then(m => m.ColdchainComponent) },
      { path: 'dispense',    loadComponent: () => import('./features/dispense/dispense.component').then(m => m.DispenseComponent) },
      { path: 'audit',       loadComponent: () => import('./features/audit/audit.component').then(m => m.AuditComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
