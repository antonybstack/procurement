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

  // Chat interface state
  protected currentMessage = signal<string>('');
  protected autoScroll = signal<boolean>(true);
  protected isComposing = signal<boolean>(false);

  // Store state
  protected messages = this.searchStore.messages;
  protected isStreaming = this.searchStore.isStreaming;
  protected canSendMessage = this.searchStore.canSendMessage;
  protected chatError = this.searchStore.chatError;

  // Sample suggestions for empty state
  protected suggestions = [
    "What products are available in our catalog?",
    "Find suppliers for electronic components",
    "Show me recent RFQ activity",
    "What are the top performing suppliers?",
    "Help me find procurement best practices"
  ];

  constructor() {
    // Auto-scroll effect when new messages arrive
    effect(() => {
      const messages = this.messages();
      if (messages.length > 0 && this.autoScroll()) {
        // Scroll will be handled in ngAfterViewChecked
      }
    });
  }

  protected sendMessage(): void {
    const message = this.currentMessage().trim();
    if (!message || !this.canSendMessage()) {
      return;
    }

    // Send message via store
    this.searchStore.sendMessage(message);

    // Clear input
    this.currentMessage.set('');

    // Enable auto-scroll for new message
    this.autoScroll.set(true);
  }

  protected useSuggestion(suggestion: string): void {
    this.currentMessage.set(suggestion);
    this.sendMessage();
  }

  protected handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  protected handleCompositionStart(): void {
    this.isComposing.set(true);
  }

  protected handleCompositionEnd(): void {
    this.isComposing.set(false);
  }

  protected copyMessage(message: any): void {
    navigator.clipboard.writeText(message.content).then(() => {
      console.log('Message copied to clipboard');
    });
  }

  protected cancelStream(): void {
    this.searchStore.cancelStream();
  }

  protected clearChat(): void {
    this.searchStore.clearChat();
    this.currentMessage.set('');
    this.autoScroll.set(true);
  }

  protected retryLastMessage(): void {
    const messages = this.messages();
    const lastUserMessage = messages
      .slice()
      .reverse()
      .find(msg => msg.role === 'user');

    if (lastUserMessage) {
      this.searchStore.sendMessage(lastUserMessage.content);
    }
  }

  protected toggleAutoScroll(): void {
    this.autoScroll.update(auto => !auto);
  }

  protected isMessageStreaming(message: any): boolean {
    return this.searchStore.isMessageStreaming(message.id);
  }

  protected onScroll(): void {
    // Handle scroll for auto-scroll detection
  }

  protected formatTimestamp(timestamp: Date): string {
    return new Date(timestamp).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
