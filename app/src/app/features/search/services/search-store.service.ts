import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SearchService } from './search.service';
import {
  ChatMessage,
  DocumentMetadata,
  SearchResult,
  SearchPageState
} from '../../../shared/models';

/**
 * Search Store Service - Provides a simplified, signal-based interface
 * for components to interact with search and chat functionality
 */
@Injectable({
  providedIn: 'root'
})
export class SearchStoreService {
  private searchService = inject(SearchService);

  // Direct access to search service signals
  readonly chatState = this.searchService.chatState;
  readonly searchState = this.searchService.searchState;
  readonly documents = this.searchService.documents;
  readonly isLoadingDocuments = this.searchService.isLoadingDocuments;
  readonly pageState = this.searchService.pageState;

  // Convenience computed signals
  readonly messages = computed(() => this.chatState().messages);
  readonly isStreaming = computed(() => this.chatState().isStreaming);
  readonly hasMessages = this.searchService.hasMessages;

  readonly searchResults = computed(() => this.searchState().searchResults);
  readonly isSearching = computed(() => this.searchState().isSearching);
  readonly hasSearchResults = this.searchService.hasSearchResults;
  readonly searchQuery = computed(() => this.searchState().searchQuery);

  readonly isActive = this.searchService.isActive;

  // Error states
  readonly chatError = computed(() => this.chatState().error);
  readonly searchError = computed(() => this.searchState().error);
  readonly hasErrors = computed(() =>
    Boolean(this.chatError() || this.searchError())
  );

  // UI-specific computed signals
  readonly canSendMessage = computed(() =>
    !this.isStreaming() && !this.isSearching()
  );

  readonly latestMessage = computed(() => {
    const messages = this.messages();
    return messages.length > 0 ? messages[messages.length - 1] : null;
  });

  readonly availableDocuments = computed(() =>
    this.documents().filter(doc => doc.status === 'processed' || doc.status === 'ready')
  );

  constructor() {
    // Auto-load documents on service initialization
    this.loadDocuments();

    // Effect to handle streaming completion logging
    effect(() => {
      const streaming = this.isStreaming();
      const currentMessageId = this.chatState().currentStreamingMessageId;

      if (!streaming && currentMessageId) {
        console.log('Chat streaming completed for message:', currentMessageId);
      }
    });
  }

  // Chat operations

  /**
   * Send a streaming chat message
   */
  sendMessage(message: string): void {
    if (!this.canSendMessage()) {
      console.warn('Cannot send message while streaming or searching');
      return;
    }

    this.searchService.streamChatCompletion(message)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (chatMessage) => {
          // Message updates are handled by the service
          console.log('Streaming message update:', chatMessage.id);
        },
        error: (error) => {
          console.error('Chat streaming error:', error);
        },
        complete: () => {
          console.log('Chat streaming completed');
        }
      });
  }

  /**
   * Cancel ongoing chat stream
   */
  cancelStream(): void {
    this.searchService.cancelStream();
  }

  /**
   * Clear all chat messages
   */
  clearChat(): void {
    this.searchService.clearChat();
  }

  // Search operations

  /**
   * Perform semantic search
   */
  search(query: string, documentFilter?: string, maxResults?: number): void {
    if (!query.trim()) {
      console.warn('Search query cannot be empty');
      return;
    }

    this.searchService.searchDocuments(query, documentFilter, maxResults)
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (response) => {
          console.log('Search completed:', response.results.length, 'results');
        },
        error: (error) => {
          console.error('Search error:', error);
        }
      });
  }

  /**
   * Clear search results
   */
  clearSearch(): void {
    this.searchService.clearSearch();
  }

  /**
   * Set document filter for searches
   */
  setDocumentFilter(documentId?: string): void {
    this.searchService.setDocumentFilter(documentId);
  }

  // Document operations

  /**
   * Load available documents
   */
  loadDocuments(): void {
    this.searchService.loadDocuments()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (documents) => {
          console.log('Loaded documents:', documents.length);
        },
        error: (error) => {
          console.error('Error loading documents:', error);
        }
      });
  }

  // Utility methods for components

  /**
   * Get message by ID
   */
  getMessageById(id: string): ChatMessage | undefined {
    return this.messages().find(msg => msg.id === id);
  }

  /**
   * Check if a message is currently streaming
   */
  isMessageStreaming(messageId: string): boolean {
    return this.chatState().currentStreamingMessageId === messageId;
  }

  /**
   * Get search result by document ID
   */
  getSearchResultsByDocument(documentId: string): SearchResult[] {
    return this.searchResults().filter(result => result.documentId === documentId);
  }

  /**
   * Get document metadata by ID
   */
  getDocumentById(id: string): DocumentMetadata | undefined {
    return this.documents().find(doc => doc.id === id);
  }

  // State management helpers

  /**
   * Reset all state to initial values
   */
  reset(): void {
    this.clearChat();
    this.clearSearch();
    this.cancelStream();
  }

  /**
   * Export current state for debugging
   */
  exportState(): SearchPageState {
    return this.pageState();
  }

  /**
   * Get a summary of current activity
   */
  getActivitySummary(): {
    messageCount: number;
    searchResultCount: number;
    documentCount: number;
    isActive: boolean;
    hasErrors: boolean;
  } {
    return {
      messageCount: this.messages().length,
      searchResultCount: this.searchResults().length,
      documentCount: this.documents().length,
      isActive: this.isActive(),
      hasErrors: this.hasErrors()
    };
  }
}
