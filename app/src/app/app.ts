import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected title = 'Procurement Management System';

  constructor(private router: Router) { }

  isSearchRoute(): boolean {
    return this.router.url.startsWith('/search');
  }
}
