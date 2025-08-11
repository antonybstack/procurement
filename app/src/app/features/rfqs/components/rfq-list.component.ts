import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent, themeQuartz, colorSchemeVariable } from 'ag-grid-community';
import { RfqService, RfqFilters } from '../services/rfq.service';
import { RfqDto, PaginatedResult } from '../../../shared/models/rfq.model';

@Component({
    selector: 'app-rfq-list',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular],
    templateUrl: './rfq-list.component.html',
    styleUrl: './rfq-list.component.css'
})
export class RfqListComponent implements OnInit {
    private rfqService = inject(RfqService);
    private router = inject(Router);

    // Signals
    rfqs = signal<RfqDto[]>([]);
    loading = signal(false);
    error = signal<string | null>(null);
    totalCount = signal(0);
    currentPage = signal(1);
    pageSize = signal(20);

    // Filters
    filters = signal<RfqFilters>({
        page: 1,
        pageSize: 20,
        search: '',
        status: '',
        fromDate: '',
        toDate: ''
    });

    // Computed
    totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
    hasNextPage = computed(() => this.currentPage() < this.totalPages());
    hasPrevPage = computed(() => this.currentPage() > 1);

    // AG Grid
    private gridApi!: GridApi;
    columnDefs: ColDef[] = [
        {
            field: 'rfqNumber',
            headerName: 'RFQ Number',
            sortable: true,
            filter: true,
            width: 150
        },
        {
            field: 'title',
            headerName: 'Title',
            sortable: true,
            filter: true,
            flex: 1
        },
        {
            field: 'status',
            headerName: 'Status',
            sortable: true,
            filter: true,
            width: 120,
            cellRenderer: (params: any) => {
                const status = params.value;
                const statusClass = this.getStatusClass(status);
                return `<span class="px-2 py-1 text-xs font-medium rounded-full ${statusClass}">${status}</span>`;
            }
        },
        {
            field: 'issueDate',
            headerName: 'Issue Date',
            sortable: true,
            filter: true,
            width: 120,
            valueFormatter: (params: any) => new Date(params.value).toLocaleDateString()
        },
        {
            field: 'dueDate',
            headerName: 'Due Date',
            sortable: true,
            filter: true,
            width: 120,
            valueFormatter: (params: any) => new Date(params.value).toLocaleDateString()
        },
        {
            field: 'totalEstimatedValue',
            headerName: 'Est. Value',
            sortable: true,
            filter: true,
            width: 120,
            valueFormatter: (params: any) => {
                if (params.value) {
                    return `${params.value.toLocaleString()} ${params.data.currency}`;
                }
                return '-';
            }
        },
        {
            field: 'lineItemsCount',
            headerName: 'Line Items',
            sortable: true,
            filter: true,
            width: 100
        },
        {
            field: 'suppliersInvited',
            headerName: 'Suppliers',
            sortable: true,
            filter: true,
            width: 100
        },
        {
            field: 'quotesReceived',
            headerName: 'Quotes',
            sortable: true,
            filter: true,
            width: 100
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
        this.loadRfqs();
    }

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }

    onRowClicked(event: any) {
        const rfqId = event.data.rfqId;
        this.router.navigate(['/rfqs', rfqId]);
    }

    loadRfqs() {
        this.loading.set(true);
        this.error.set(null);

        this.rfqService.getRfqs(this.filters()).subscribe({
            next: (result: PaginatedResult<RfqDto>) => {
                this.rfqs.set(result.data);
                this.totalCount.set(result.totalCount);
                this.currentPage.set(result.page);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load RFQs. Please try again.');
                this.loading.set(false);
                console.error('Error loading RFQs:', err);
            }
        });
    }

    onSearchInputChange(value: string) {
        this.filters.update(f => ({ ...f, search: value, page: 1 }));
        this.loadRfqs();
    }

    onStatusInputChange(value: string) {
        this.filters.update(f => ({ ...f, status: value, page: 1 }));
        this.loadRfqs();
    }

    onFromDateInputChange(value: string) {
        this.filters.update(f => ({ ...f, fromDate: value, page: 1 }));
        this.loadRfqs();
    }

    onToDateInputChange(value: string) {
        this.filters.update(f => ({ ...f, toDate: value, page: 1 }));
        this.loadRfqs();
    }

    onPageChange(page: number) {
        this.filters.update(f => ({ ...f, page }));
        this.loadRfqs();
    }

    clearFilters() {
        this.filters.set({
            page: 1,
            pageSize: 20,
            search: '',
            status: '',
            fromDate: '',
            toDate: ''
        });
        this.loadRfqs();
    }

    private getStatusClass(status: string): string {
        switch (status.toLowerCase()) {
            case 'draft':
                return 'bg-gray-100 text-gray-800';
            case 'open':
                return 'bg-blue-100 text-blue-800';
            case 'closed':
                return 'bg-red-100 text-red-800';
            case 'awarded':
                return 'bg-green-100 text-green-800';
            default:
                return 'bg-gray-100 text-gray-800';
        }
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
