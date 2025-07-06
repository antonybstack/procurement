import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ItemService } from '../services/item.service';
import { Item } from '../../../shared/models/item.model';

@Component({
    selector: 'app-item-detail',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './item-detail.component.html',
    styleUrls: ['./item-detail.component.css']
})
export class ItemDetailComponent implements OnInit {
    private itemService = inject(ItemService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);

    // Signals
    item = signal<Item | null>(null);
    loading = signal(false);
    error = signal<string | null>(null);

    ngOnInit(): void {
        this.loadItem();
    }

    loadItem(): void {
        const id = this.route.snapshot.paramMap.get('id');
        if (!id) {
            this.error.set('Invalid item ID');
            return;
        }

        this.loading.set(true);
        this.error.set(null);

        this.itemService.getItemById(+id).subscribe({
            next: (item: Item) => {
                this.item.set(item);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load item details. Please try again.');
                this.loading.set(false);
                console.error('Error loading item:', err);
            }
        });
    }

    goBack(): void {
        this.router.navigate(['/items']);
    }

    onEdit(): void {
        const item = this.item();
        if (item) {
            this.router.navigate(['/items', item.itemId, 'edit']);
        }
    }

    getStatusClass(isActive: boolean): string {
        return isActive ? 'status-active' : 'status-inactive';
    }

    getStatusText(isActive: boolean): string {
        return isActive ? 'Active' : 'Inactive';
    }

    formatCurrency(amount?: number): string {
        if (amount === undefined || amount === null) return 'Not specified';
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(amount);
    }

    getCreatedAtDisplay(createdAt: string): string {
        return new Date(createdAt).toLocaleString();
    }
} 