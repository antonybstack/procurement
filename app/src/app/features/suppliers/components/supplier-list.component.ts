import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';
import { SupplierService, SupplierFilters } from '../services/supplier.service';
import { SupplierDto } from '../../../shared/models/supplier.model';
import { PaginatedResult } from '../../../shared/models/rfq.model';

@Component({
    selector: 'app-supplier-list',
    standalone: true,
    imports: [CommonModule, FormsModule, AgGridAngular],
    templateUrl: './supplier-list.component.html',
    styleUrl: './supplier-list.component.css'
})
export class SupplierListComponent implements OnInit {
    private supplierService = inject(SupplierService);
    private router = inject(Router);

    // Signals
    suppliers = signal<SupplierDto[]>([]);
    loading = signal(false);
    error = signal<string | null>(null);
    totalCount = signal(0);
    currentPage = signal(1);
    pageSize = signal(20);
    countries = signal<string[]>([]);

    // Filters
    filters = signal<SupplierFilters>({
        page: 1,
        pageSize: 20,
        search: '',
        country: '',
        minRating: undefined,
        isActive: undefined
    });

    // Computed
    totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
    hasNextPage = computed(() => this.currentPage() < this.totalPages());
    hasPrevPage = computed(() => this.currentPage() > 1);

    // AG Grid
    private gridApi!: GridApi;
    columnDefs: ColDef[] = [
        {
            field: 'supplierCode',
            headerName: 'Supplier Code',
            sortable: true,
            filter: true,
            width: 150
        },
        {
            field: 'companyName',
            headerName: 'Company Name',
            sortable: true,
            filter: true,
            flex: 1
        },
        {
            field: 'contactName',
            headerName: 'Contact Name',
            sortable: true,
            filter: true,
            width: 150
        },
        {
            field: 'email',
            headerName: 'Email',
            sortable: true,
            filter: true,
            width: 200
        },
        {
            field: 'phone',
            headerName: 'Phone',
            sortable: true,
            filter: true,
            width: 130
        },
        {
            field: 'city',
            headerName: 'City',
            sortable: true,
            filter: true,
            width: 120
        },
        {
            field: 'state',
            headerName: 'State',
            sortable: true,
            filter: true,
            width: 100
        },
        {
            field: 'country',
            headerName: 'Country',
            sortable: true,
            filter: true,
            width: 120
        },
        {
            field: 'rating',
            headerName: 'Rating',
            sortable: true,
            filter: true,
            width: 100,
            cellRenderer: (params: any) => {
                if (params.value) {
                    return `<div class="flex items-center">
            <span class="text-yellow-400">★</span>
            <span class="ml-1">${params.value}</span>
          </div>`;
                }
                return '-';
            }
        },
        {
            field: 'isActive',
            headerName: 'Status',
            sortable: true,
            filter: true,
            width: 100,
            cellRenderer: (params: any) => {
                const isActive = params.value;
                const statusClass = isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800';
                const statusText = isActive ? 'Active' : 'Inactive';
                return `<span class="px-2 py-1 text-xs font-medium rounded-full ${statusClass}">${statusText}</span>`;
            }
        },
        {
            field: 'createdAt',
            headerName: 'Created At',
            sortable: true,
            filter: true,
            width: 130,
            valueFormatter: (params: any) => new Date(params.value).toLocaleDateString()
        }
    ];

    defaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true
    };

    ngOnInit() {
        this.loadSuppliers();
        this.loadCountries();
    }

    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
    }

    onRowClicked(event: any) {
        const supplierId = event.data.supplierId;
        this.router.navigate(['/suppliers', supplierId]);
    }

    loadSuppliers() {
        this.loading.set(true);
        this.error.set(null);

        this.supplierService.getSuppliers(this.filters()).subscribe({
            next: (result: PaginatedResult<SupplierDto>) => {
                this.suppliers.set(result.data);
                this.totalCount.set(result.totalCount);
                this.currentPage.set(result.page);
                this.loading.set(false);
            },
            error: (err) => {
                this.error.set('Failed to load suppliers. Please try again.');
                this.loading.set(false);
                console.error('Error loading suppliers:', err);
            }
        });
    }

    loadCountries() {
        this.supplierService.getCountries().subscribe({
            next: (countries: string[]) => {
                this.countries.set(countries);
            },
            error: (err) => {
                console.error('Error loading countries:', err);
            }
        });
    }

    onSearchInputChange(value: string) {
        this.filters.update(f => ({ ...f, search: value, page: 1 }));
        this.loadSuppliers();
    }

    onCountryInputChange(value: string) {
        this.filters.update(f => ({ ...f, country: value, page: 1 }));
        this.loadSuppliers();
    }

    onRatingInputChange(value: string) {
        this.filters.update(f => ({ ...f, minRating: value ? +value : undefined, page: 1 }));
        this.loadSuppliers();
    }

    onStatusInputChange(value: string) {
        this.filters.update(f => ({ ...f, isActive: value === '' ? undefined : value === 'true', page: 1 }));
        this.loadSuppliers();
    }

    onPageChange(page: number) {
        this.filters.update(f => ({ ...f, page }));
        this.loadSuppliers();
    }

    clearFilters() {
        this.filters.set({
            page: 1,
            pageSize: 20,
            search: '',
            country: '',
            minRating: undefined,
            isActive: undefined
        });
        this.loadSuppliers();
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