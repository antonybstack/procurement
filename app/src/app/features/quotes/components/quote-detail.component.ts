import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { QuoteService } from '../services/quote.service';
import { QuoteDto } from '../../../shared/models/quote.model';

@Component({
    selector: 'app-quote-detail',
    standalone: true,
    imports: [CommonModule],
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
            'Pending': 'bg-yellow-100 text-yellow-800',
            'Submitted': 'bg-blue-100 text-blue-800',
            'Awarded': 'bg-green-100 text-green-800',
            'Rejected': 'bg-red-100 text-red-800',
            'Expired': 'bg-gray-100 text-gray-800'
        };
        return statusClasses[status] || 'bg-gray-100 text-gray-800';
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