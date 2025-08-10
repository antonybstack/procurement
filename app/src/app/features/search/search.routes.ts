import { Routes } from '@angular/router';
import { SearchLayoutComponent } from './components/search-layout.component';
import { SearchInterfaceComponent } from './components/search-interface.component';

export const searchRoutes: Routes = [
  {
    path: '',
    component: SearchLayoutComponent,
    children: [
      {
        path: '',
        component: SearchInterfaceComponent
      },
      {
        path: '**',
        redirectTo: ''
      }
    ]
  }
];
