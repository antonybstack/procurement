import { Routes } from '@angular/router';

export const quotesRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./components/quote-list.component').then(m => m.QuoteListComponent)
    },
    {
        path: ':id',
        loadComponent: () => import('./components/quote-detail.component').then(m => m.QuoteDetailComponent)
    },
    {
        path: ':id/edit',
        loadComponent: () => import('./components/quote-edit.component').then(m => m.QuoteEditComponent)
    }
]; 