import {Component, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {Router, RouterLink, RouterLinkActive, RouterOutlet} from '@angular/router';
import {provideGlobalGridOptions} from 'ag-grid-community';

// provide localeText to all grids via global options
provideGlobalGridOptions({
  enableCellTextSelection: true,
});

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  // Mobile menu state
  isMobileMenuOpen = false;
  // Starfield data: 3 layers with 200 stars each
  starsLayers: { cx: string; cy: string; r: number; delay: number }[][] = [];
  private router = inject(Router);

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
        stars.push({cx, cy, r, delay});
      }
      this.starsLayers.push(stars);
    }
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen = false;
  }

  isSearchRoute(): boolean {
    return this.router.url.startsWith('/search');
  }
}
