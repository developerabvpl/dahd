import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'vendor-register',
    loadComponent: () => import('./features/vendor-register/vendor-register.component').then(m => m.VendorRegisterComponent)
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
      { path: 'stock',       loadComponent: () => import('./features/stock/stock.component').then(m => m.StockComponent) },
      { path: 'indents',     loadComponent: () => import('./features/indents/indents.component').then(m => m.IndentsComponent) },
      { path: 'coldchain',   loadComponent: () => import('./features/coldchain/coldchain.component').then(m => m.ColdchainComponent) },
      { path: 'coldchain-analytics', loadComponent: () => import('./features/coldchain-analytics/coldchain-analytics.component').then(m => m.ColdchainAnalyticsComponent) },
      { path: 'dispense',    loadComponent: () => import('./features/dispense/dispense.component').then(m => m.DispenseComponent) },
      { path: 'campaigns',   loadComponent: () => import('./features/campaigns/campaigns.component').then(m => m.CampaignsComponent) },
      { path: 'redistribution', loadComponent: () => import('./features/redistribution/redistribution.component').then(m => m.RedistributionComponent) },
      { path: 'consumption', loadComponent: () => import('./features/consumption/consumption.component').then(m => m.ConsumptionComponent) },
      { path: 'vendor',      loadComponent: () => import('./features/vendor-portal/vendor-portal.component').then(m => m.VendorPortalComponent) },
      { path: 'vendors',     loadComponent: () => import('./features/vendors-admin/vendors-admin.component').then(m => m.VendorsAdminComponent) },
      { path: 'audit',       loadComponent: () => import('./features/audit/audit.component').then(m => m.AuditComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
