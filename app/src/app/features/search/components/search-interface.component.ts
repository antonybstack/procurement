import { Component, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchStoreService } from '../services/search-store.service';

@Component({
  selector: 'app-search-interface',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './search-interface.component.html',
  styleUrl: './search-interface.component.css'
})
export class SearchInterfaceComponent {
  private searchStore = inject(SearchStoreService);

  // Local UI state
  protected activeTab = signal<'chat' | 'search' | 'documents'>('chat');
  protected isMobile = signal<boolean>(false);
  protected sidebarCollapsed = signal<boolean>(false);

  // Computed state from store
  protected isActive = this.searchStore.isActive;
  protected hasErrors = this.searchStore.hasErrors;
  protected chatError = this.searchStore.chatError;
  protected searchError = this.searchStore.searchError;

  constructor() {
    // Check for mobile viewport
    this.checkMobile();
    window.addEventListener('resize', () => this.checkMobile());

    // Effect to handle errors
    effect(() => {
      const chatErr = this.chatError();
      const searchErr = this.searchError();

      if (chatErr) {
        console.error('Chat error:', chatErr);
      }

      if (searchErr) {
        console.error('Search error:', searchErr);
      }
    });
  }

  protected setActiveTab(tab: 'chat' | 'search' | 'documents'): void {
    this.activeTab.set(tab);

    // Auto-collapse sidebar on mobile when switching tabs
    if (this.isMobile()) {
      this.sidebarCollapsed.set(true);
    }
  }

  protected toggleSidebar(): void {
    this.sidebarCollapsed.update(collapsed => !collapsed);
  }

  protected clearErrors(): void {
    // Trigger a new search/chat to clear error states
    // This will be handled by individual components
  }

  private checkMobile(): void {
    this.isMobile.set(window.innerWidth < 768);

    // Auto-collapse sidebar on mobile
    if (window.innerWidth < 768) {
      this.sidebarCollapsed.set(true);
    }
  }
}
