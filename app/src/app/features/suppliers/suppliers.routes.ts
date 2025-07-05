import { Routes } from '@angular/router';

export const suppliersRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./components/supplier-list.component').then(m => m.SupplierListComponent)
    },
    {
        path: ':id',
        loadComponent: () => import('./components/supplier-detail.component').then(m => m.SupplierDetailComponent)
    }
]; 