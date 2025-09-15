import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { SupplierService } from '../services/supplier.service';
import { SupplierDetailDto } from '../../../shared/models';

@Component({
  selector: 'app-supplier-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './supplier-detail.component.html',
  styleUrl: './supplier-detail.component.css'
})
export class SupplierDetailComponent implements OnInit {
  private supplierService = inject(SupplierService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // Signals
  supplier = signal<SupplierDetailDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadSupplier();
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
      next: (supplier: SupplierDetailDto) => {
        this.supplier.set(supplier);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load supplier details. Please try again.');
        this.loading.set(false);
        console.error('Error loading supplier:', err);
      }
    });
  }

  goBack() {
    this.router.navigate(['/suppliers']);
  }

  editSupplier() {
    const supplierId = this.supplier()?.supplierId;
    if (supplierId) {
      this.router.navigate(['/suppliers', supplierId, 'edit']);
    }
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'brutalist-status-awarded' : 'brutalist-status-closed';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'active' : 'inactive';
  }

  getRatingStars(rating?: number): string {
    if (!rating) return 'No rating';
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }

  getCreatedAtDisplay(createdAt: string): string {
    return new Date(createdAt).toLocaleString();
  }
}
