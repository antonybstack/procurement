import { Routes } from '@angular/router';

export const rfqsRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./components/rfq-list.component').then(m => m.RfqListComponent)
    },
    {
        path: ':id',
        loadComponent: () => import('./components/rfq-detail.component').then(m => m.RfqDetailComponent)
    }
]; 