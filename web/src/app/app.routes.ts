import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'drugs',
    loadComponent: () => import('./features/drugs/drugs.component').then(m => m.DrugsComponent)
  },
  {
    path: 'warehouses',
    loadComponent: () => import('./features/warehouses/warehouses.component').then(m => m.WarehousesComponent)
  },
  {
    path: 'facilities',
    loadComponent: () => import('./features/facilities/facilities.component').then(m => m.FacilitiesComponent)
  },
  {
    path: 'batches',
    loadComponent: () => import('./features/batches/batches.component').then(m => m.BatchesComponent)
  },
  {
    path: 'indents',
    loadComponent: () => import('./features/indents/indents.component').then(m => m.IndentsComponent)
  },
  {
    path: 'coldchain',
    loadComponent: () => import('./features/coldchain/coldchain.component').then(m => m.ColdchainComponent)
  },
  {
    path: 'dispense',
    loadComponent: () => import('./features/dispense/dispense.component').then(m => m.DispenseComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
