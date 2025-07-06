import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { QuoteService } from '../services/quote.service';
import { QuoteDto } from '../../../shared/models/quote.model';

@Component({
    selector: 'app-quote-edit',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule],
    templateUrl: './quote-edit.component.html',
    styleUrl: './quote-edit.component.css'
})
export class QuoteEditComponent implements OnInit {
    private quoteService = inject(QuoteService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private fb = inject(FormBuilder);

    // Signals
    quote = signal<QuoteDto | null>(null);
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);
    statuses = signal<string[]>([]);

    // Form
    editForm!: FormGroup;

    ngOnInit() {
        this.initializeForm();
        this.loadQuote();
        this.loadStatuses();
    }

    private initializeForm() {
        this.editForm = this.fb.group({
            quoteNumber: ['', [Validators.required, Validators.maxLength(50)]],
            status: ['', [Validators.required]],
            unitPrice: [0, [Validators.required, Validators.min(0)]],
            totalPrice: [0, [Validators.required, Validators.min(0)]],
            quantityOffered: [0, [Validators.required, Validators.min(1)]],
            deliveryDate: [''],
            paymentTerms: ['', [Validators.maxLength(100)]],
            warrantyPeriodMonths: [null, [Validators.min(0)]],
            technicalComplianceNotes: [''],
            validUntilDate: ['']
        });
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
                this.populateForm(quote);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load quote details. Please try again.');
                this.loading.set(false);
                console.error('Error loading quote:', err);
            }
        });
    }

    loadStatuses() {
        this.quoteService.getStatuses().subscribe({
            next: (statuses) => {
                this.statuses.set(statuses);
            },
            error: (error) => {
                console.error('Error loading statuses:', error);
            }
        });
    }

    private populateForm(quote: QuoteDto) {
        this.editForm.patchValue({
            quoteNumber: quote.quoteNumber,
            status: quote.status,
            unitPrice: quote.unitPrice,
            totalPrice: quote.totalPrice,
            quantityOffered: quote.quantityOffered,
            deliveryDate: quote.deliveryDate || '',
            paymentTerms: quote.paymentTerms || '',
            warrantyPeriodMonths: quote.warrantyPeriodMonths || null,
            technicalComplianceNotes: quote.technicalComplianceNotes || '',
            validUntilDate: quote.validUntilDate || ''
        });
    }

    onSubmit() {
        if (this.editForm.invalid) {
            this.markFormGroupTouched();
            return;
        }

        const quoteId = this.quote()?.quoteId;
        if (!quoteId) {
            this.error.set('Invalid Quote ID');
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        const updateData = this.editForm.value;

        // Convert empty strings to null for optional fields
        Object.keys(updateData).forEach(key => {
            if (updateData[key] === '') {
                updateData[key] = null;
            }
        });

        this.quoteService.updateQuote(quoteId, updateData).subscribe({
            next: (updatedQuote) => {
                this.quote.set(updatedQuote);
                this.saving.set(false);
                this.router.navigate(['/quotes', quoteId]);
            },
            error: (err) => {
                this.error.set('Failed to update quote. Please try again.');
                this.saving.set(false);
                console.error('Error updating quote:', err);
            }
        });
    }

    onCancel() {
        const quoteId = this.quote()?.quoteId;
        if (quoteId) {
            this.router.navigate(['/quotes', quoteId]);
        } else {
            this.router.navigate(['/quotes']);
        }
    }

    private markFormGroupTouched() {
        Object.keys(this.editForm.controls).forEach(key => {
            const control = this.editForm.get(key);
            control?.markAsTouched();
        });
    }

    isFieldInvalid(fieldName: string): boolean {
        const field = this.editForm.get(fieldName);
        return !!(field && field.invalid && field.touched);
    }

    getFieldError(fieldName: string): string {
        const field = this.editForm.get(fieldName);
        if (field && field.errors) {
            if (field.errors['required']) return 'This field is required';
            if (field.errors['email']) return 'Please enter a valid email address';
            if (field.errors['maxlength']) return `Maximum length is ${field.errors['maxlength'].requiredLength} characters`;
            if (field.errors['min']) return `Minimum value is ${field.errors['min'].min}`;
            if (field.errors['max']) return `Maximum value is ${field.errors['max'].max}`;
        }
        return '';
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
} 