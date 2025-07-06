import { Routes } from '@angular/router';

export const itemsRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./components/item-list.component').then(m => m.ItemListComponent)
    },
    {
        path: 'new',
        loadComponent: () => import('./components/item-edit.component').then(m => m.ItemEditComponent)
    },
    {
        path: ':id',
        loadComponent: () => import('./components/item-detail.component').then(m => m.ItemDetailComponent)
    },
    {
        path: ':id/edit',
        loadComponent: () => import('./components/item-edit.component').then(m => m.ItemEditComponent)
    }
]; 