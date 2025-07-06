import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ItemService } from '../services/item.service';
import { Item } from '../../../shared/models/item.model';

@Component({
    selector: 'app-item-edit',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './item-edit.component.html',
    styleUrls: ['./item-edit.component.css']
})
export class ItemEditComponent implements OnInit {
    private itemService = inject(ItemService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);

    // Signals
    item = signal<Partial<Item>>({});
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);
    categories = signal<string[]>([]);

    // Form state
    isEditMode = false;
    itemId: number | null = null;

    ngOnInit() {
        this.loadCategories();
        this.initializeForm();
    }

    private initializeForm() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id && id !== 'new') {
            this.isEditMode = true;
            this.itemId = +id;
            this.loadItem(+id);
        } else {
            this.isEditMode = false;
            this.item.set({
                itemCode: '',
                description: '',
                category: 'Electronics',
                unitOfMeasure: '',
                standardCost: undefined,
                minOrderQuantity: 1,
                leadTimeDays: 30,
                isActive: true
            });
        }
    }

    private loadCategories() {
        this.itemService.getCategories().subscribe({
            next: (categories) => {
                this.categories.set(categories);
            },
            error: (error) => {
                console.error('Error loading categories:', error);
            }
        });
    }

    private loadItem(id: number) {
        this.loading.set(true);
        this.error.set(null);

        this.itemService.getItemById(id).subscribe({
            next: (item: Item) => {
                this.item.set(item);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load item. Please try again.');
                this.loading.set(false);
                console.error('Error loading item:', err);
            }
        });
    }

    onSubmit() {
        if (!this.validateForm()) {
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        const itemData = this.item();

        if (this.isEditMode && this.itemId) {
            this.itemService.updateItem(this.itemId, itemData).subscribe({
                next: (updatedItem) => {
                    this.saving.set(false);
                    this.router.navigate(['/items', updatedItem.itemId]);
                },
                error: (err) => {
                    this.error.set('Failed to update item. Please try again.');
                    this.saving.set(false);
                    console.error('Error updating item:', err);
                }
            });
        } else {
            this.itemService.createItem(itemData).subscribe({
                next: (newItem) => {
                    this.saving.set(false);
                    this.router.navigate(['/items', newItem.itemId]);
                },
                error: (err) => {
                    this.error.set('Failed to create item. Please try again.');
                    this.saving.set(false);
                    console.error('Error creating item:', err);
                }
            });
        }
    }

    onCancel() {
        if (this.isEditMode && this.itemId) {
            this.router.navigate(['/items', this.itemId]);
        } else {
            this.router.navigate(['/items']);
        }
    }

    private validateForm(): boolean {
        const item = this.item();

        if (!item.itemCode?.trim()) {
            this.error.set('Item code is required');
            return false;
        }

        if (!item.description?.trim()) {
            this.error.set('Description is required');
            return false;
        }

        if (!item.category?.trim()) {
            this.error.set('Category is required');
            return false;
        }

        if (!item.unitOfMeasure?.trim()) {
            this.error.set('Unit of measure is required');
            return false;
        }

        if (item.minOrderQuantity && item.minOrderQuantity < 1) {
            this.error.set('Minimum order quantity must be at least 1');
            return false;
        }

        if (item.leadTimeDays && item.leadTimeDays < 0) {
            this.error.set('Lead time days cannot be negative');
            return false;
        }

        this.error.set(null);
        return true;
    }

    updateField(field: keyof Item, value: any) {
        this.item.update(item => ({ ...item, [field]: value }));
    }
} 