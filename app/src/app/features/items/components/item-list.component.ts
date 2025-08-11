import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent, themeQuartz, colorSchemeVariable } from 'ag-grid-community';
import { ItemService, ItemFilters } from '../services/item.service';
import { ItemDto } from '../../../shared/models/item.model';
import { PaginatedResult } from '../../../shared/models/rfq.model';

@Component({
    selector: 'app-item-list',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular],
    templateUrl: './item-list.component.html',
    styleUrl: './item-list.component.css'
})
export class ItemListComponent implements OnInit {
    private itemService = inject(ItemService);
    private router = inject(Router);

    // Signals
    items = signal<ItemDto[]>([]);
    loading = signal(false);
    error = signal<string | null>(null);
    totalCount = signal(0);
    currentPage = signal(1);
    pageSize = signal(20);

    // Filters
    filters = signal<ItemFilters>({
        page: 1,
        pageSize: 20,
        search: '',
        category: ''
    });

    // Computed
    totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
    hasNextPage = computed(() => this.currentPage() < this.totalPages());
    hasPrevPage = computed(() => this.currentPage() > 1);

    // AG Grid
    private gridApi!: GridApi;
    columnDefs: ColDef[] = [
        {
            field: 'itemCode',
            headerName: 'Item Code',
            sortable: true,
            filter: true,
            width: 150
        },
        {
            field: 'description',
            headerName: 'Description',
            sortable: true,
            filter: true,
            flex: 1
        },
        {
            field: 'category',
            headerName: 'Category',
            sortable: true,
            filter: true,
            width: 120
        },
        {
            field: 'unitOfMeasure',
            headerName: 'UOM',
            sortable: true,
            filter: true,
            width: 100
        },
        {
            field: 'standardCost',
            headerName: 'Standard Cost',
            sortable: true,
            filter: true,
            width: 130,
            valueFormatter: (params: any) => {
                if (params.value) {
                    return params.value.toLocaleString();
                }
                return '-';
            }
        },
        {
            field: 'minOrderQuantity',
            headerName: 'Min Order Qty',
            sortable: true,
            filter: true,
            width: 120
        },
        {
            field: 'leadTimeDays',
            headerName: 'Lead Time (Days)',
            sortable: true,
            filter: true,
            width: 140
        },
        {
            field: 'isActive',
            headerName: 'Status',
            sortable: true,
            filter: true,
            width: 100,
            cellRenderer: (params: any) => {
                const isActive = params.value;
                const statusClass = this.getStatusClass(isActive);
                const statusText = this.getStatusText(isActive);
                return `<span class="px-2 py-1 text-xs font-medium rounded-full ${statusClass}">${statusText}</span>`;
            }
        }
    ];

    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true
    };

    // AG Grid theme configured to respond to data-ag-theme-mode
    gridTheme = themeQuartz.withPart(colorSchemeVariable);

    ngOnInit() {
        this.loadItems();
    }

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }

    onRowClicked(event: any) {
        const itemId = event.data.itemId;
        this.router.navigate(['/items', itemId]);
    }

    loadItems() {
        this.loading.set(true);
        this.error.set(null);

        this.itemService.getItems(this.filters()).subscribe({
            next: (result: PaginatedResult<ItemDto>) => {
                this.items.set(result.data);
                this.totalCount.set(result.totalCount);
                this.currentPage.set(result.page);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load items. Please try again.');
                this.loading.set(false);
                console.error('Error loading items:', err);
            }
        });
    }

    onSearchInputChange(value: string) {
        this.filters.update(f => ({ ...f, search: value, page: 1 }));
        this.loadItems();
    }

    onCategoryInputChange(value: string) {
        this.filters.update(f => ({ ...f, category: value, page: 1 }));
        this.loadItems();
    }

    onPageChange(page: number) {
        this.filters.update(f => ({ ...f, page }));
        this.loadItems();
    }

    clearFilters() {
        this.filters.set({
            page: 1,
            pageSize: 20,
            search: '',
            category: ''
        });
        this.loadItems();
    }

    onCreateNew() {
        this.router.navigate(['/items/new']);
    }

    private getStatusClass(isActive: boolean): string {
        return isActive ? 'status-active' : 'status-inactive';
    }

    private getStatusText(isActive: boolean): string {
        return isActive ? 'Active' : 'Inactive';
    }

    getPageNumbers(): number[] {
        const pages: number[] = [];
        const totalPages = this.totalPages();
        const currentPage = this.currentPage();

        let start = Math.max(1, currentPage - 2);
        let end = Math.min(totalPages, currentPage + 2);

        if (end - start < 4) {
            if (start === 1) {
                end = Math.min(totalPages, start + 4);
            } else {
                start = Math.max(1, end - 4);
            }
        }

        for (let i = start; i <= end; i++) {
            pages.push(i);
        }

        return pages;
    }

    protected readonly Math = Math;
}
