import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Router} from '@angular/router';
import {AgGridAngular} from 'ag-grid-angular';
import {
  CellValueChangedEvent,
  ColDef,
  colorSchemeVariable,
  GridApi,
  GridReadyEvent,
  themeQuartz
} from 'ag-grid-community';
import {SupplierFilters, SupplierService, SupplierUpdateDto, VectorSearchFilters} from '../services/supplier.service';
import {SupplierDto} from '../../../shared/models/supplier.model';
import {PaginatedResult} from '../../../shared/models/rfq.model';

@Component({
  selector: 'app-supplier-list',
  standalone: true,
  imports: [CommonModule, FormsModule, AgGridAngular],
  templateUrl: './supplier-list.component.html',
  styleUrl: './supplier-list.component.css'
})
export class SupplierListComponent implements OnInit {
  // Signals
  suppliers = signal<SupplierDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(20);
  countries = signal<string[]>([]);
  // Search state
  vectorSearchEnabled = signal(false);
  keywordSearchEnabled = signal(false);
  searchQuery = signal('');
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
  columnDefs: ColDef[] = [
    {
      field: 'supplierCode',
      headerName: 'Supplier Code',
      sortable: true,
      filter: true,
      width: 150,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'companyName',
      headerName: 'Company Name',
      sortable: true,
      filter: true,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'contactName',
      headerName: 'Contact Name',
      sortable: true,
      filter: true,
      width: 150,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'email',
      headerName: 'Email',
      sortable: true,
      filter: true,
      width: 200,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'phone',
      headerName: 'Phone',
      sortable: true,
      filter: true,
      width: 130,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'city',
      headerName: 'City',
      sortable: true,
      filter: true,
      width: 120,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'state',
      headerName: 'State',
      sortable: true,
      filter: true,
      width: 100,
      editable: true,
      cellEditor: 'agTextCellEditor'
    },
    {
      field: 'country',
      headerName: 'Country',
      sortable: true,
      filter: true,
      width: 120,
      editable: true,
      cellEditor: 'agSelectCellEditor',
      cellEditorParams: {
        values: this.countries
      }
    },
    {
      field: 'rating',
      headerName: 'Rating',
      sortable: true,
      filter: true,
      width: 100,
      editable: true,
      cellEditor: 'agNumberCellEditor',
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
      editable: true,
      cellEditor: 'agSelectCellEditor',
      cellEditorParams: {
        values: [true, false]
      },
      cellRenderer: (params: any) => {
        const isActive = params.value;
        const statusClass = isActive ? 'brutalist-status-awarded' : 'brutalist-status-closed';
        const statusText = isActive ? 'active' : 'inactive';
        return `<span class="${statusClass}">${statusText}</span>`;
      }
    },
    {
      field: 'createdAt',
      headerName: 'Created At',
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
  // AG Grid theme configured to respond to data-ag-theme-mode
  gridTheme = themeQuartz
    .withPart(colorSchemeVariable)
    .withParams({
      backgroundColor: 'rgba(0,0,0,0)',
      chromeBackgroundColor: 'rgba(0,0,0,0)'
    });
  protected readonly Math = Math;
  private supplierService = inject(SupplierService);
  private router = inject(Router);
  // AG Grid
  private gridApi!: GridApi;

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

  onCellValueChanged(event: CellValueChangedEvent) {
    const supplierId = event.data.supplierId;
    const field = event.colDef.field;
    const newValue = event.newValue;
    const oldValue = event.oldValue;

    // Don't save if value hasn't changed
    if (newValue === oldValue) {
      return;
    }

    // Get the current supplier data to ensure we have all required fields
    const currentSupplier = this.suppliers().find(s => s.supplierId === supplierId);
    if (!currentSupplier) {
      console.error('Supplier not found for update');
      return;
    }

    // Create update object with current data and the changed field
    const updateData: SupplierUpdateDto = {
      supplierCode: currentSupplier.supplierCode,
      companyName: currentSupplier.companyName,
      isActive: currentSupplier.isActive,
      contactName: currentSupplier.contactName,
      email: currentSupplier.email,
      phone: currentSupplier.phone,
      address: currentSupplier.address,
      city: currentSupplier.city,
      state: currentSupplier.state,
      country: currentSupplier.country,
      postalCode: currentSupplier.postalCode,
      taxId: currentSupplier.taxId,
      paymentTerms: currentSupplier.paymentTerms,
      creditLimit: currentSupplier.creditLimit,
      rating: currentSupplier.rating
    };

    // Update only the changed field
    if (field) {
      (updateData as any)[field] = newValue;
    }

    // Save immediately
    this.supplierService.updateSupplier(supplierId, updateData).subscribe({
      next: (updatedSupplier) => {
        // Update the local data
        this.suppliers.update(suppliers =>
          suppliers.map(s =>
            s.supplierId === supplierId ? updatedSupplier : s
          )
        );

        // Show success feedback (optional)
        console.log(`Supplier ${supplierId} updated successfully`);
      },
      error: (error) => {
        // Revert the change in the grid
        event.node.setDataValue(event.column.getColId(), oldValue);

        // Show error message
        this.error.set(`Failed to update supplier. Please try again.`);
        console.error('Error updating supplier:', error);

        // Clear error after 3 seconds
        setTimeout(() => this.error.set(null), 3000);
      }
    });
  }

  loadSuppliers() {
    this.loading.set(true);
    this.error.set(null);

    // Use vector search if enabled and there's a search query
    if (this.vectorSearchEnabled() && this.searchQuery().trim()) {
      const vectorFilters: VectorSearchFilters = {
        searchValue: this.searchQuery(),
        top: this.filters().pageSize || 20
      };

      this.supplierService.vectorSearch(vectorFilters).subscribe({
        next: (result: SupplierDto[]) => {
          // const res = (result as any).map((x: { record: any; }) => x.record);
          this.suppliers.set(result);
          this.totalCount.set(result.length);
          this.currentPage.set(1);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('Failed to perform AI search. Please try again.');
          this.loading.set(false);
          console.error('Error performing vector search:', err);
        }
      });
    } else if (this.keywordSearchEnabled() && this.searchQuery().trim()) {
      // Use keyword search if enabled and there's a search query
      const keywordFilters: VectorSearchFilters = {
        searchValue: this.searchQuery(),
        top: this.filters().pageSize || 20
      };

      this.supplierService.keywordSearch(keywordFilters).subscribe({
        next: (result: SupplierDto[]) => {
          this.suppliers.set(result);
          this.totalCount.set(result.length);
          this.currentPage.set(1);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('Failed to perform full-text search. Please try again.');
          this.loading.set(false);
          console.error('Error performing keyword search:', err);
        }
      });
    } else {
      // Use regular search
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
  }

  loadCountries() {
    this.supplierService.getCountries().subscribe({
      next: (countries: string[]) => {
        this.countries.set(countries);
        // Update the country column editor params
        const countryCol = this.columnDefs.find(col => col.field === 'country');
        if (countryCol) {
          countryCol.cellEditorParams = {values: countries};
        }
      },
      error: (err) => {
        console.error('Error loading countries:', err);
      }
    });
  }

  onSearchInputChange(value: string) {
    this.filters.update(f => ({...f, search: value, page: 1}));
    this.loadSuppliers();
  }

  onSearchQueryChange(value: string) {
    this.searchQuery.set(value);

    if (this.vectorSearchEnabled() || this.keywordSearchEnabled()) {
      // For vector or keyword search, update page to 1 and search
      this.filters.update(f => ({...f, page: 1}));
    } else {
      // For regular search, update the filters search term
      this.filters.update(f => ({...f, search: value, page: 1}));
    }

    this.loadSuppliers();
  }

  toggleVectorSearch() {
    const wasEnabled = this.vectorSearchEnabled();
    this.vectorSearchEnabled.set(!wasEnabled);

    // Disable keyword search if enabling vector search
    if (!wasEnabled && this.vectorSearchEnabled()) {
      this.keywordSearchEnabled.set(false);
    }

    // Clear any existing error when switching modes
    this.error.set(null);

    // If switching to vector search, clear regular filters that don't apply
    if (!wasEnabled && this.vectorSearchEnabled()) {
      // Keep the search query but clear other filters for vector search
      this.filters.update(f => ({
        ...f,
        search: '', // Clear regular search as we'll use searchQuery
        page: 1
      }));
    } else if (wasEnabled && !this.vectorSearchEnabled()) {
      // Switching back to regular search - update regular search with current query
      this.filters.update(f => ({
        ...f,
        search: this.searchQuery(),
        page: 1
      }));
    }

    // Reload with new search mode
    this.loadSuppliers();
  }

  toggleKeywordSearch() {
    const wasEnabled = this.keywordSearchEnabled();
    this.keywordSearchEnabled.set(!wasEnabled);

    // Disable vector search if enabling keyword search
    if (!wasEnabled && this.keywordSearchEnabled()) {
      this.vectorSearchEnabled.set(false);
    }

    // Clear any existing error when switching modes
    this.error.set(null);

    // If switching to keyword search, clear regular filters that don't apply
    if (!wasEnabled && this.keywordSearchEnabled()) {
      // Keep the search query but clear other filters for keyword search
      this.filters.update(f => ({
        ...f,
        search: '', // Clear regular search as we'll use searchQuery
        page: 1
      }));
    } else if (wasEnabled && !this.keywordSearchEnabled()) {
      // Switching back to regular search - update regular search with current query
      this.filters.update(f => ({
        ...f,
        search: this.searchQuery(),
        page: 1
      }));
    }

    // Reload with new search mode
    this.loadSuppliers();
  }

  onCountryInputChange(value: string) {
    this.filters.update(f => ({...f, country: value, page: 1}));
    this.loadSuppliers();
  }

  onRatingInputChange(value: string) {
    const rating = value ? parseInt(value) : undefined;
    this.filters.update(f => ({...f, minRating: rating, page: 1}));
    this.loadSuppliers();
  }

  onStatusInputChange(value: string) {
    const isActive = value === 'active' ? true : value === 'inactive' ? false : undefined;
    this.filters.update(f => ({...f, isActive, page: 1}));
    this.loadSuppliers();
  }

  onPageChange(page: number) {
    this.filters.update(f => ({...f, page}));
    this.loadSuppliers();
  }

  clearFilters() {
    this.searchQuery.set('');
    this.vectorSearchEnabled.set(false);
    this.keywordSearchEnabled.set(false);
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
    const totalPages = this.totalPages();
    const currentPage = this.currentPage();
    const pageNumbers: number[] = [];

    const startPage = Math.max(1, currentPage - 2);
    const endPage = Math.min(totalPages, currentPage + 2);

    for (let i = startPage; i <= endPage; i++) {
      pageNumbers.push(i);
    }

    return pageNumbers;
  }
}
