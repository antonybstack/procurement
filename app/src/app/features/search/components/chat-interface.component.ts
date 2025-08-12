import {
  Component,
  inject,
  signal,
  effect,
  ViewChild,
  ElementRef,
  AfterViewChecked
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchStoreService } from '../services/search-store.service';
import type { ChatMessage } from '../../../shared/models';

@Component({
  selector: 'app-chat-interface',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-interface.component.html',
  styleUrl: './chat-interface.component.css'
})
export class ChatInterfaceComponent implements AfterViewChecked {
  private searchStore = inject(SearchStoreService);

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  @ViewChild('messageInput') private messageInput!: ElementRef;

  // Local state
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
    "Tell me about supplier Supplier 1 Corp.",
    "What is Supplier 113 Corp. capable of?",
    "Give me the performance highlights for supplier Supplier 469 Corp.",
  ];

  private shouldScrollToBottom = false;

  constructor() {
    // Auto-scroll effect when new messages arrive
    effect(() => {
      const messages = this.messages();
      if (messages.length > 0 && this.autoScroll()) {
        this.shouldScrollToBottom = true;
      }
    });

    // Focus input when streaming stops
    effect(() => {
      const streaming = this.isStreaming();
      if (!streaming && this.messageInput?.nativeElement) {
        setTimeout(() => this.messageInput.nativeElement.focus(), 100);
      }
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
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

  protected copyMessage(message: ChatMessage): void {
    navigator.clipboard.writeText(message.content).then(() => {
      // Could show a toast notification here
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
    if (this.autoScroll()) {
      this.scrollToBottom();
    }
  }

  protected isMessageStreaming(message: ChatMessage): boolean {
    return this.searchStore.isMessageStreaming(message.id);
  }

  private scrollToBottom(): void {
    if (this.messagesContainer?.nativeElement) {
      const container = this.messagesContainer.nativeElement;
      container.scrollTop = container.scrollHeight;
    }
  }

  protected onScroll(): void {
    if (this.messagesContainer?.nativeElement) {
      const container = this.messagesContainer.nativeElement;
      const isAtBottom = container.scrollHeight - container.clientHeight <= container.scrollTop + 1;
      this.autoScroll.set(isAtBottom);
    }
  }

  protected formatTimestamp(timestamp: Date): string {
    return new Date(timestamp).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
