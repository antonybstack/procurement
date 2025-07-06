import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent, CellValueChangedEvent } from 'ag-grid-community';
import { QuoteService, QuoteFilters } from '../services/quote.service';
import { QuoteSummaryDto, QuoteUpdateDto } from '../../../shared/models/quote.model';
import { PaginatedResult } from '../../../shared/models/rfq.model';

@Component({
    selector: 'app-quote-list',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular],
    templateUrl: './quote-list.component.html',
    styleUrl: './quote-list.component.css'
})
export class QuoteListComponent implements OnInit {
    private quoteService = inject(QuoteService);
    private router = inject(Router);

    // Signals
    quotes = signal<QuoteSummaryDto[]>([]);
    loading = signal(false);
    error = signal<string | null>(null);
    totalCount = signal(0);
    currentPage = signal(1);
    pageSize = signal(20);
    statuses = signal<string[]>([]);

    // Filters
    filters = signal<QuoteFilters>({
        page: 1,
        pageSize: 20,
        search: '',
        status: '',
        rfqId: undefined,
        supplierId: undefined,
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
            field: 'quoteNumber',
            headerName: 'Quote Number',
            sortable: true,
            filter: true,
            width: 150,
            editable: true,
            cellEditor: 'agTextCellEditor'
        },
        {
            field: 'supplierName',
            headerName: 'Supplier',
            sortable: true,
            filter: true,
            flex: 1,
            editable: false
        },
        {
            field: 'rfqNumber',
            headerName: 'RFQ Number',
            sortable: true,
            filter: true,
            width: 150,
            editable: false
        },
        {
            field: 'itemDescription',
            headerName: 'Item Description',
            sortable: true,
            filter: true,
            width: 200,
            editable: false
        },
        {
            field: 'status',
            headerName: 'Status',
            sortable: true,
            filter: true,
            width: 120,
            editable: true,
            cellEditor: 'agSelectCellEditor',
            cellEditorParams: {
                values: this.statuses
            },
            cellRenderer: (params: any) => {
                const status = params.value;
                const statusClasses: { [key: string]: string } = {
                    'Pending': 'bg-yellow-100 text-yellow-800',
                    'Submitted': 'bg-blue-100 text-blue-800',
                    'Awarded': 'bg-green-100 text-green-800',
                    'Rejected': 'bg-red-100 text-red-800',
                    'Expired': 'bg-gray-100 text-gray-800'
                };
                const statusClass = statusClasses[status] || 'bg-gray-100 text-gray-800';
                return `<span class="px-2 py-1 text-xs font-medium rounded-full ${statusClass}">${status}</span>`;
            }
        },
        {
            field: 'unitPrice',
            headerName: 'Unit Price',
            sortable: true,
            filter: true,
            width: 120,
            editable: true,
            cellEditor: 'agNumberCellEditor',
            valueFormatter: (params: any) => `$${params.value?.toFixed(2)}`
        },
        {
            field: 'totalPrice',
            headerName: 'Total Price',
            sortable: true,
            filter: true,
            width: 120,
            editable: true,
            cellEditor: 'agNumberCellEditor',
            valueFormatter: (params: any) => `$${params.value?.toFixed(2)}`
        },
        {
            field: 'quantityOffered',
            headerName: 'Quantity',
            sortable: true,
            filter: true,
            width: 100,
            editable: true,
            cellEditor: 'agNumberCellEditor'
        },
        {
            field: 'deliveryDate',
            headerName: 'Delivery Date',
            sortable: true,
            filter: true,
            width: 130,
            editable: true,
            cellEditor: 'agDateCellEditor',
            valueFormatter: (params: any) => params.value ? new Date(params.value).toLocaleDateString() : '-'
        },
        {
            field: 'submittedDate',
            headerName: 'Submitted Date',
            sortable: true,
            filter: true,
            width: 130,
            editable: false,
            valueFormatter: (params: any) => new Date(params.value).toLocaleDateString()
        }
    ];

    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true
    };

    ngOnInit() {
        this.loadQuotes();
        this.loadStatuses();
    }

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }

    onRowClicked(event: any) {
        const quoteId = event.data.quoteId;
        this.router.navigate(['/quotes', quoteId]);
    }

    onCellValueChanged(event: CellValueChangedEvent) {
        const quoteId = event.data.quoteId;
        const field = event.colDef.field;
        const newValue = event.newValue;
        const oldValue = event.oldValue;

        // Don't save if value hasn't changed
        if (newValue === oldValue) {
            return;
        }

        // Create update DTO with only the changed field
        const updateData: QuoteUpdateDto = {
            quoteNumber: event.data.quoteNumber,
            status: event.data.status,
            unitPrice: event.data.unitPrice,
            totalPrice: event.data.totalPrice,
            quantityOffered: event.data.quantityOffered,
            deliveryDate: event.data.deliveryDate,
            paymentTerms: event.data.paymentTerms,
            warrantyPeriodMonths: event.data.warrantyPeriodMonths,
            technicalComplianceNotes: event.data.technicalComplianceNotes,
            validUntilDate: event.data.validUntilDate
        };

        // Update the specific field
        (updateData as any)[field!] = newValue;

        this.quoteService.updateQuote(quoteId, updateData).subscribe({
            next: () => {
                console.log(`Updated quote ${quoteId} field ${field} to ${newValue}`);
            },
            error: (error) => {
                console.error('Error updating quote:', error);
                // Revert the change in the grid
                event.node.setDataValue(field!, oldValue);
            }
        });
    }

    loadQuotes() {
        this.loading.set(true);
        this.error.set(null);

        this.quoteService.getQuotes(this.filters()).subscribe({
            next: (result: PaginatedResult<QuoteSummaryDto>) => {
                this.quotes.set(result.data);
                this.totalCount.set(result.totalCount);
                this.currentPage.set(result.page);
                this.pageSize.set(result.pageSize);
                this.loading.set(false);
            },
            error: (error) => {
                console.error('Error loading quotes:', error);
                this.error.set('Failed to load quotes');
                this.loading.set(false);
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

    onSearchInputChange(value: string) {
        this.filters.update(f => ({ ...f, search: value, page: 1 }));
        this.loadQuotes();
    }

    onStatusInputChange(value: string) {
        this.filters.update(f => ({ ...f, status: value, page: 1 }));
        this.loadQuotes();
    }

    onRfqIdInputChange(value: string) {
        const rfqId = value ? parseInt(value) : undefined;
        this.filters.update(f => ({ ...f, rfqId, page: 1 }));
        this.loadQuotes();
    }

    onSupplierIdInputChange(value: string) {
        const supplierId = value ? parseInt(value) : undefined;
        this.filters.update(f => ({ ...f, supplierId, page: 1 }));
        this.loadQuotes();
    }

    onFromDateInputChange(value: string) {
        this.filters.update(f => ({ ...f, fromDate: value, page: 1 }));
        this.loadQuotes();
    }

    onToDateInputChange(value: string) {
        this.filters.update(f => ({ ...f, toDate: value, page: 1 }));
        this.loadQuotes();
    }

    onPageChange(page: number) {
        this.filters.update(f => ({ ...f, page }));
        this.loadQuotes();
    }

    clearFilters() {
        this.filters.set({
            page: 1,
            pageSize: 20,
            search: '',
            status: '',
            rfqId: undefined,
            supplierId: undefined,
            fromDate: '',
            toDate: ''
        });
        this.loadQuotes();
    }

    getPageNumbers(): number[] {
        const totalPages = this.totalPages();
        const currentPage = this.currentPage();
        const pages: number[] = [];

        const start = Math.max(1, currentPage - 2);
        const end = Math.min(totalPages, currentPage + 2);

        for (let i = start; i <= end; i++) {
            pages.push(i);
        }

        return pages;
    }

    protected readonly Math = Math;
} 