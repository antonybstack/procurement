import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchStoreService } from '../services/search-store.service';
import type { SearchResult, DocumentMetadata } from '../../../shared/models';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-results.component.html',
  styleUrl: './search-results.component.css'
})
export class SearchResultsComponent {
  private searchStore = inject(SearchStoreService);

  // Local UI state
  protected searchQuery = signal<string>('');
  protected selectedDocument = signal<string>('');
  protected maxResults = signal<number>(5);
  protected expandedResults = signal<Set<string>>(new Set());

  // Store state
  protected searchResults = this.searchStore.searchResults;
  protected isSearching = this.searchStore.isSearching;
  protected searchError = this.searchStore.searchError;
  protected hasSearchResults = this.searchStore.hasSearchResults;
  protected documents = this.searchStore.documents;
  protected availableDocuments = this.searchStore.availableDocuments;
  protected currentQuery = this.searchStore.searchQuery;

  // Computed state
  protected groupedResults = computed(() => {
    const results = this.searchResults();
    const grouped = new Map<string, SearchResult[]>();

    results.forEach(result => {
      const docId = result.documentId;
      if (!grouped.has(docId)) {
        grouped.set(docId, []);
      }
      grouped.get(docId)!.push(result);
    });

    return grouped;
  });

  protected filteredResults = computed(() => {
    const results = this.searchResults();
    const selectedDoc = this.selectedDocument();

    if (!selectedDoc) {
      return results;
    }

    return results.filter(result => result.documentId === selectedDoc);
  });

  protected resultStats = computed(() => {
    const results = this.searchResults();
    const documents = new Set(results.map(r => r.documentId));

    return {
      totalResults: results.length,
      documentCount: documents.size,
      averageSimilarity: results.length > 0
        ? results.reduce((sum, r) => sum + r.similarity, 0) / results.length
        : 0
    };
  });

  constructor() { }

  protected performSearch(): void {
    const query = this.searchQuery().trim();
    if (!query) {
      return;
    }

    const documentFilter = this.selectedDocument() || undefined;
    const maxResults = this.maxResults();

    this.searchStore.search(query, documentFilter, maxResults);
  }

  protected clearSearch(): void {
    this.searchStore.clearSearch();
    this.searchQuery.set('');
    this.selectedDocument.set('');
    this.expandedResults.set(new Set());
  }

  protected setDocumentFilter(documentId: string): void {
    this.selectedDocument.set(documentId);
    this.searchStore.setDocumentFilter(documentId || undefined);

    // Auto-search if there's a current query
    if (this.currentQuery()) {
      this.performSearch();
    }
  }

  protected toggleResultExpansion(resultId: string): void {
    this.expandedResults.update(expanded => {
      const newExpanded = new Set(expanded);
      if (newExpanded.has(resultId)) {
        newExpanded.delete(resultId);
      } else {
        newExpanded.add(resultId);
      }
      return newExpanded;
    });
  }

  protected isResultExpanded(resultId: string): boolean {
    return this.expandedResults().has(resultId);
  }

  protected getDocumentName(documentId: string): string {
    const doc = this.documents().find(d => d.id === documentId);
    return doc?.fileName || documentId;
  }

  protected formatSimilarity(similarity: number): string {
    return Math.round(similarity * 100) + '%';
  }

  protected getSimilarityColor(similarity: number): string {
    if (similarity >= 0.8) return 'text-green-600 bg-green-100';
    if (similarity >= 0.6) return 'text-yellow-600 bg-yellow-100';
    return 'text-red-600 bg-red-100';
  }

  protected highlightText(text: string, query: string): string {
    if (!query.trim()) return text;

    const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    return text.replace(regex, '<mark class="bg-yellow-200 px-1 rounded">$1</mark>');
  }

  protected truncateText(text: string, maxLength: number = 200): string {
    if (text.length <= maxLength) return text;
    return text.slice(0, maxLength) + '...';
  }

  protected copyResult(result: SearchResult): void {
    const textToCopy = `Document: ${this.getDocumentName(result.documentId)}
Page: ${result.pageNumber}
Content: ${result.text}
Similarity: ${this.formatSimilarity(result.similarity)}`;

    navigator.clipboard.writeText(textToCopy).then(() => {
      console.log('Search result copied to clipboard');
    });
  }

  protected handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.performSearch();
    }
  }

  protected generateResultId(result: SearchResult, index: number): string {
    return `${result.documentId}-${result.pageNumber}-${index}`;
  }
}
