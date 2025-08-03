import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ItemService } from '../services/item.service';
import { ItemDto } from '../../../shared/models/item.model';
import { AiService } from '../services/ai.service';
import { SupplierPerformanceAnalysisDto } from '../../../shared/models/ai-analysis.model';

@Component({
    selector: 'app-item-detail',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './item-detail.component.html',
    styleUrls: ['./item-detail.component.css']
})
export class ItemDetailComponent implements OnInit {
    private itemService = inject(ItemService);
    private aiService = inject(AiService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);

    // Signals
    item = signal<ItemDto | null>(null);
    loading = signal(false);
    error = signal<string | null>(null);

    analysis = signal<SupplierPerformanceAnalysisDto | null>(null);
    loadingAnalysis = signal(false);
    analysisError = signal<string | null>(null);

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
            next: (item: ItemDto) => {
                this.item.set(item);
                this.loading.set(false);
                this.loadAnalysis(item.itemId);
            },
            error: (err) => {
                this.error.set('Failed to load item details. Please try again.');
                this.loading.set(false);
                console.error('Error loading item:', err);
            }
        });
    }

    loadAnalysis(itemId: number): void {
        this.loadingAnalysis.set(true);
        this.analysisError.set(null);

        this.aiService.getPerformanceAnalysis(itemId).subscribe({
            next: (data) => {
                this.analysis.set(data);
                this.loadingAnalysis.set(false);
            },
            error: (err) => {
                this.analysisError.set('Failed to load AI performance analysis.');
                this.loadingAnalysis.set(false);
                console.error('Error loading AI analysis:', err);
            }
        });
    }

    retryAnalysis(): void {
        const item = this.item();
        if (item) {
            this.loadAnalysis(item.itemId);
        }
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
