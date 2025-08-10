import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SearchStoreService } from '../services/search-store.service';
import type { DocumentMetadata } from '../../../shared/models';

@Component({
  selector: 'app-document-browser',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './document-browser.component.html',
  styleUrl: './document-browser.component.css'
})
export class DocumentBrowserComponent {
  private searchStore = inject(SearchStoreService);

  // Expose Object for template use
  protected Object = Object;

  // Local state
  protected filterText = signal<string>('');
  protected sortBy = signal<'name' | 'date' | 'size' | 'status'>('date');
  protected sortDirection = signal<'asc' | 'desc'>('desc');
  protected selectedDocument = signal<string>('');

  // Store state
  protected documents = this.searchStore.documents;
  protected isLoadingDocuments = this.searchStore.isLoadingDocuments;

  // Computed state
  protected filteredDocuments = computed(() => {
    const documents = this.documents();
    const filter = this.filterText().toLowerCase().trim();

    let filtered = documents;

    // Apply text filter
    if (filter) {
      filtered = documents.filter(doc =>
        doc.fileName.toLowerCase().includes(filter) ||
        doc.status.toLowerCase().includes(filter)
      );
    }

    // Apply sorting
    const sortBy = this.sortBy();
    const direction = this.sortDirection();

    filtered.sort((a, b) => {
      let comparison = 0;

      switch (sortBy) {
        case 'name':
          comparison = a.fileName.localeCompare(b.fileName);
          break;
        case 'date':
          comparison = new Date(a.uploadedAt).getTime() - new Date(b.uploadedAt).getTime();
          break;
        case 'size':
          comparison = a.chunkCount - b.chunkCount;
          break;
        case 'status':
          comparison = a.status.localeCompare(b.status);
          break;
      }

      return direction === 'desc' ? -comparison : comparison;
    });

    return filtered;
  });

  protected documentStats = computed(() => {
    const documents = this.documents();
    const statusCounts = documents.reduce((acc, doc) => {
      acc[doc.status] = (acc[doc.status] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    const totalChunks = documents.reduce((sum, doc) => sum + doc.chunkCount, 0);

    return {
      total: documents.length,
      statusCounts,
      totalChunks,
      averageChunks: documents.length > 0 ? Math.round(totalChunks / documents.length) : 0
    };
  });

  constructor() {
    // Load documents when component initializes
    this.loadDocuments();
  }

  protected loadDocuments(): void {
    this.searchStore.loadDocuments();
  }

  protected refreshDocuments(): void {
    this.searchStore.loadDocuments();
  }

  protected setSortBy(field: 'name' | 'date' | 'size' | 'status'): void {
    if (this.sortBy() === field) {
      // Toggle direction if same field
      this.sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    } else {
      // Set new field with default direction
      this.sortBy.set(field);
      this.sortDirection.set(field === 'date' ? 'desc' : 'asc');
    }
  }

  protected clearFilter(): void {
    this.filterText.set('');
  }

  protected selectDocument(documentId: string): void {
    const currentSelected = this.selectedDocument();
    const newSelected = currentSelected === documentId ? '' : documentId;
    this.selectedDocument.set(newSelected);

    // Set document filter in search store
    this.searchStore.setDocumentFilter(newSelected || undefined);
  }

  protected formatFileSize(chunkCount: number): string {
    if (chunkCount === 0) return '0 chunks';
    if (chunkCount === 1) return '1 chunk';
    return `${chunkCount.toLocaleString()} chunks`;
  }

  protected formatDate(date: Date): string {
    return new Date(date).toLocaleDateString([], {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  protected getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'processed':
      case 'ready':
        return 'text-green-700 bg-green-100';
      case 'processing':
      case 'uploading':
        return 'text-yellow-700 bg-yellow-100';
      case 'failed':
      case 'error':
        return 'text-red-700 bg-red-100';
      default:
        return 'text-gray-700 bg-gray-100';
    }
  }

  protected getStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'processed':
      case 'ready':
        return 'M5 13l4 4L19 7'; // Check icon
      case 'processing':
      case 'uploading':
        return 'M12 2v20m8-10H4'; // Clock icon path would be better
      case 'failed':
      case 'error':
        return 'M6 18L18 6M6 6l12 12'; // X icon
      default:
        return 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z'; // Document icon
    }
  }

  protected getSortIcon(field: string): string {
    if (this.sortBy() !== field) {
      return 'M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4'; // Sort icon
    }

    return this.sortDirection() === 'asc'
      ? 'M3 4h13M3 8h9m-9 4h6m4 0l4-4m0 0l4 4m-4-4v12' // Sort up
      : 'M3 4h13M3 8h9m-9 4h9m5-4v12m0 0l-4-4m4 4l4-4'; // Sort down
  }

  protected isDocumentSelected(documentId: string): boolean {
    return this.selectedDocument() === documentId;
  }

  protected getFileExtension(fileName: string): string {
    const parts = fileName.split('.');
    return parts.length > 1 ? parts[parts.length - 1].toUpperCase() : '';
  }

  protected getFileIcon(fileName: string): string {
    const ext = this.getFileExtension(fileName).toLowerCase();

    switch (ext) {
      case 'pdf':
        return 'text-red-600';
      case 'doc':
      case 'docx':
        return 'text-blue-600';
      case 'txt':
      case 'md':
        return 'text-gray-600';
      case 'xlsx':
      case 'xls':
        return 'text-green-600';
      default:
        return 'text-gray-500';
    }
  }

  protected getSelectedDocumentName(): string {
    const selectedId = this.selectedDocument();
    const doc = this.documents().find(d => d.id === selectedId);
    return doc?.fileName || '';
  }
}
