import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { ThemeService } from './shared/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private router = inject(Router);
  private themeService = inject(ThemeService);

  // Public getters for template
  isDarkMode = () => this.themeService.isDark();
  getTheme = () => this.themeService.theme();

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  isSearchRoute(): boolean {
    return this.router.url.startsWith('/search');
  }
}
