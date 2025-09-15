import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef, colorSchemeVariable, themeQuartz} from 'ag-grid-community';
import {RfqService} from '../services/rfq.service';
import {RfqDetailDto} from '../../../shared/models/rfq.model';

@Component({
  selector: 'app-rfq-detail',
  standalone: true,
  imports: [CommonModule, AgGridAngular, RouterLink],
  templateUrl: './rfq-detail.component.html',
  styleUrls: ['./rfq-detail.component.css']
})
export class RfqDetailComponent implements OnInit {
  Date = Date;
  // Signals
  rfq = signal<RfqDetailDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  // AG Grid for Line Items
  lineItemColumnDefs: ColDef[] = [
    {field: 'lineNumber', headerName: 'Line #', width: 80},
    {
      field: 'item.itemCode',
      headerName: 'Item Code',
      width: 120,
      cellStyle: {cursor: 'pointer', color: '#2563eb'},
      onCellClicked: (params) => this.navigateToItem(params.data.item.itemId)
    },
    {field: 'item.description', headerName: 'Description'},
    {field: 'quantityRequired', headerName: 'Qty Required', width: 120},
    {field: 'unitOfMeasure', headerName: 'UOM', width: 100},
    {
      field: 'estimatedUnitCost', headerName: 'Est. Unit Cost', width: 130,
      valueFormatter: (params: any) => params.value ? params.value.toLocaleString() : '-'
    },
    {
      field: 'deliveryDate', headerName: 'Delivery Date', width: 130,
      valueFormatter: (params: any) => params.value ? new Date(params.value).toLocaleDateString() : '-'
    }
  ];
  // AG Grid for Quotes
  quoteColumnDefs: ColDef[] = [
    {
      field: 'quoteNumber',
      headerName: 'Quote #',
      width: 120,
      cellStyle: {cursor: 'pointer', color: '#2563eb'},
      onCellClicked: (params) => this.navigateToQuote(params.data.quoteId)
    },
    {
      field: 'supplier.companyName',
      headerName: 'Supplier',
      cellStyle: {cursor: 'pointer', color: '#2563eb'},
      onCellClicked: (params) => this.navigateToSupplier(params.data.supplier.supplierId)
    },
    {
      field: 'status', headerName: 'Status', width: 100,
      cellRenderer: (params: any) => {
        const status = params.value;
        const statusClass = this.getQuoteStatusClass(status);
        return `<span class="px-2 py-1 text-xs font-medium rounded-full ${statusClass}">${status}</span>`;
      }
    },
    {
      field: 'unitPrice', headerName: 'Unit Price', width: 120,
      valueFormatter: (params: any) => params.value ? params.value.toLocaleString() : '-'
    },
    {
      field: 'totalPrice', headerName: 'Total Price', width: 120,
      valueFormatter: (params: any) => params.value ? params.value.toLocaleString() : '-'
    },
    {
      field: 'deliveryDate', headerName: 'Delivery Date', width: 130,
      valueFormatter: (params: any) => params.value ? new Date(params.value).toLocaleDateString() : '-'
    },
    {
      field: 'submittedDate', headerName: 'Submitted', width: 130,
      valueFormatter: (params: any) => params.value ? new Date(params.value).toLocaleDateString() : '-'
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
  private rfqService = inject(RfqService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  ngOnInit() {
    this.loadRfq();
  }

  loadRfq() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Invalid RFQ ID');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.rfqService.getRfqById(+id).subscribe({
      next: (rfq: RfqDetailDto) => {
        this.rfq.set(rfq);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load RFQ details. Please try again.');
        this.loading.set(false);
        console.error('Error loading RFQ:', err);
      }
    });
  }

  navigateToItem(id: number) {
    if (id) this.router.navigate(['/items', id]);
  }

  navigateToQuote(id: number) {
    if (id) this.router.navigate(['/quotes', id]);
  }

  navigateToSupplier(id: number) {
    if (id) this.router.navigate(['/suppliers', id]);
  }

  goBack() {
    this.router.navigate(['/rfqs']);
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'draft':
        return 'brutalist-status-draft';
      case 'open':
        return 'brutalist-status-open';
      case 'closed':
        return 'brutalist-status-closed';
      case 'awarded':
        return 'brutalist-status-awarded';
      default:
        return 'brutalist-status-draft';
    }
  }

  getAwardDateDisplay(awardDate?: string): string {
    if (!awardDate) return 'Not awarded';
    return new Date(awardDate).toLocaleDateString();
  }

  getEstimatedValueDisplay(value?: number, currency?: string): string {
    if (!value) return 'Not specified';
    return `${value.toLocaleString()} ${currency || ''}`;
  }

  getCreatedAtDisplay(createdAt: string): string {
    return new Date(createdAt).toLocaleString();
  }

  getSupplierLocationDisplay(city?: string, state?: string, country?: string): string {
    return [city, state, country].filter(Boolean).join(', ');
  }

  private getQuoteStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'submitted':
        return 'brutalist-status-open';
      case 'under_review':
        return 'brutalist-status-open';
      case 'accepted':
        return 'brutalist-status-awarded';
      case 'rejected':
        return 'brutalist-status-closed';
      default:
        return 'brutalist-status-draft';
    }
  }
}
