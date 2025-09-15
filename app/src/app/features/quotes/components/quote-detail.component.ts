import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { QuoteService } from '../services/quote.service';
import { QuoteDto } from '../../../shared/models/quote.model';

@Component({
  selector: 'app-quote-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './quote-detail.component.html',
  styleUrl: './quote-detail.component.css'
})
export class QuoteDetailComponent implements OnInit {
  private quoteService = inject(QuoteService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // Signals
  quote = signal<QuoteDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadQuote();
  }

  loadQuote() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Invalid Quote ID');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.quoteService.getQuoteById(+id).subscribe({
      next: (quote: QuoteDto) => {
        this.quote.set(quote);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load quote details. Please try again.');
        this.loading.set(false);
        console.error('Error loading quote:', err);
      }
    });
  }

  goBack() {
    this.router.navigate(['/quotes']);
  }

  editQuote() {
    const quoteId = this.quote()?.quoteId;
    if (quoteId) {
      this.router.navigate(['/quotes', quoteId, 'edit']);
    }
  }

  getStatusClass(status: string): string {
    const statusClasses: { [key: string]: string } = {
      'Pending': 'brutalist-status-draft',
      'Submitted': 'brutalist-status-open',
      'Awarded': 'brutalist-status-awarded',
      'Rejected': 'brutalist-status-closed',
      'Expired': 'brutalist-status-closed'
    };
    return statusClasses[status] || 'brutalist-status-draft';
  }

  getStatusText(status: string): string {
    return status;
  }

  getCreatedAtDisplay(createdAt: string): string {
    return new Date(createdAt).toLocaleString();
  }

  getSubmittedDateDisplay(submittedDate: string): string {
    return new Date(submittedDate).toLocaleString();
  }

  getDeliveryDateDisplay(deliveryDate?: string): string {
    return deliveryDate ? new Date(deliveryDate).toLocaleDateString() : 'Not specified';
  }

  getValidUntilDateDisplay(validUntilDate?: string): string {
    return validUntilDate ? new Date(validUntilDate).toLocaleDateString() : 'Not specified';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
