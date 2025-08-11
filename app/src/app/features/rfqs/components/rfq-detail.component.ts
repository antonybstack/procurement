import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, themeQuartz, colorSchemeVariable } from 'ag-grid-community';
import { RfqService } from '../services/rfq.service';
import { RfqDetailDto } from '../../../shared/models/rfq.model';

@Component({
  selector: 'app-rfq-detail',
  standalone: true,
  imports: [CommonModule, AgGridAngular, RouterLink],
  templateUrl: './rfq-detail.component.html',
  styleUrls: ['./rfq-detail.component.css']
})
export class RfqDetailComponent implements OnInit {
  Date = Date;
  private rfqService = inject(RfqService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  // Signals
  rfq = signal<RfqDetailDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  // AG Grid for Line Items
  lineItemColumnDefs: ColDef[] = [
    { field: 'lineNumber', headerName: 'Line #', width: 80 },
    {
      field: 'item.itemCode',
      headerName: 'Item Code',
      width: 120,
      cellStyle: { cursor: 'pointer', color: '#2563eb' },
      onCellClicked: (params) => this.navigateToItem(params.data.item.itemId)
    },
    { field: 'item.description', headerName: 'Description', flex: 1 },
    { field: 'quantityRequired', headerName: 'Qty Required', width: 120 },
    { field: 'unitOfMeasure', headerName: 'UOM', width: 100 },
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
      cellStyle: { cursor: 'pointer', color: '#2563eb' },
      onCellClicked: (params) => this.navigateToQuote(params.data.quoteId)
    },
    {
      field: 'supplier.companyName',
      headerName: 'Supplier',
      flex: 1,
      cellStyle: { cursor: 'pointer', color: '#2563eb' },
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
  gridTheme = themeQuartz.withPart(colorSchemeVariable);

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

  private getQuoteStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'submitted':
        return 'bg-blue-100 text-blue-800';
      case 'under_review':
        return 'bg-yellow-100 text-yellow-800';
      case 'accepted':
        return 'bg-green-100 text-green-800';
      case 'rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
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
}
