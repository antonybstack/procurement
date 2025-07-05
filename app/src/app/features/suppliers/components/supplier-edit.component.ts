import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SupplierService } from '../services/supplier.service';
import { SupplierDto } from '../../../shared/models/supplier.model';

@Component({
    selector: 'app-supplier-edit',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule],
    templateUrl: './supplier-edit.component.html',
    styleUrl: './supplier-edit.component.css'
})
export class SupplierEditComponent implements OnInit {
    private supplierService = inject(SupplierService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private fb = inject(FormBuilder);

    // Signals
    supplier = signal<SupplierDto | null>(null);
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);

    // Form
    editForm!: FormGroup;

    ngOnInit() {
        this.initializeForm();
        this.loadSupplier();
    }

    private initializeForm() {
        this.editForm = this.fb.group({
            supplierCode: ['', [Validators.required, Validators.maxLength(20)]],
            companyName: ['', [Validators.required, Validators.maxLength(255)]],
            contactName: ['', [Validators.maxLength(255)]],
            email: ['', [Validators.email, Validators.maxLength(255)]],
            phone: ['', [Validators.maxLength(50)]],
            address: [''],
            city: ['', [Validators.maxLength(100)]],
            state: ['', [Validators.maxLength(100)]],
            country: ['', [Validators.maxLength(100)]],
            postalCode: ['', [Validators.maxLength(20)]],
            taxId: ['', [Validators.maxLength(50)]],
            paymentTerms: ['', [Validators.maxLength(100)]],
            creditLimit: [null, [Validators.min(0)]],
            rating: [null, [Validators.min(1), Validators.max(5)]],
            isActive: [true]
        });
    }

    loadSupplier() {
        const id = this.route.snapshot.paramMap.get('id');
        if (!id) {
            this.error.set('Invalid Supplier ID');
            return;
        }

        this.loading.set(true);
        this.error.set(null);

        this.supplierService.getSupplierById(+id).subscribe({
            next: (supplier: SupplierDto) => {
                this.supplier.set(supplier);
                this.populateForm(supplier);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load supplier details. Please try again.');
                this.loading.set(false);
                console.error('Error loading supplier:', err);
            }
        });
    }

    private populateForm(supplier: SupplierDto) {
        this.editForm.patchValue({
            supplierCode: supplier.supplierCode,
            companyName: supplier.companyName,
            contactName: supplier.contactName || '',
            email: supplier.email || '',
            phone: supplier.phone || '',
            address: supplier.address || '',
            city: supplier.city || '',
            state: supplier.state || '',
            country: supplier.country || '',
            postalCode: supplier.postalCode || '',
            taxId: supplier.taxId || '',
            paymentTerms: supplier.paymentTerms || '',
            creditLimit: supplier.creditLimit || null,
            rating: supplier.rating || null,
            isActive: supplier.isActive
        });
    }

    onSubmit() {
        if (this.editForm.invalid) {
            this.markFormGroupTouched();
            return;
        }

        const supplierId = this.supplier()?.supplierId;
        if (!supplierId) {
            this.error.set('Invalid Supplier ID');
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

        this.supplierService.updateSupplier(supplierId, updateData).subscribe({
            next: (updatedSupplier) => {
                this.supplier.set(updatedSupplier);
                this.saving.set(false);
                this.router.navigate(['/suppliers', supplierId]);
            },
            error: (err) => {
                this.error.set('Failed to update supplier. Please try again.');
                this.saving.set(false);
                console.error('Error updating supplier:', err);
            }
        });
    }

    onCancel() {
        const supplierId = this.supplier()?.supplierId;
        if (supplierId) {
            this.router.navigate(['/suppliers', supplierId]);
        } else {
            this.router.navigate(['/suppliers']);
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
} 