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

  // Starfield data: 3 layers with 200 stars each
  starsLayers: { cx: string; cy: string; r: number; delay: number }[][] = [];

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  isSearchRoute(): boolean {
    return this.router.url.startsWith('/search');
  }

  constructor() {
    const numberOfLayers = 3;
    const starsPerLayer = 200;
    for (let layerIndex = 0; layerIndex < numberOfLayers; layerIndex += 1) {
      const stars: { cx: string; cy: string; r: number; delay: number }[] = [];
      for (let i = 0; i < starsPerLayer; i += 1) {
        const cx = `${Math.round(Math.random() * 10000) / 100}%`;
        const cy = `${Math.round(Math.random() * 10000) / 100}%`;
        const r = Math.round((Math.random() + 0.2) * 6) / 10; // 0.2 - 0.8 for smaller stars
        const delay = Math.round(Math.random() * 600) / 100; // 0 - 6s
        stars.push({ cx, cy, r, delay });
      }
      this.starsLayers.push(stars);
    }
  }
}
