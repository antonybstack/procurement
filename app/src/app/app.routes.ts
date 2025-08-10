import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/rfqs',
    pathMatch: 'full'
  },
  {
    path: 'rfqs',
    loadChildren: () => import('./features/rfqs/rfqs.routes').then(m => m.rfqsRoutes)
  },
  {
    path: 'suppliers',
    loadChildren: () => import('./features/suppliers/suppliers.routes').then(m => m.suppliersRoutes)
  },
  {
    path: 'quotes',
    loadChildren: () => import('./features/quotes/quotes.routes').then(m => m.quotesRoutes)
  },
  {
    path: 'items',
    loadChildren: () => import('./features/items/items.routes').then(m => m.itemsRoutes)
  },
  {
    path: 'search',
    loadChildren: () => import('./features/search/search.routes').then(m => m.searchRoutes)
  }
];
