import { SupplierDto, ItemDto, QuoteDto, RfqDto } from './';

export interface PurchaseOrderDto {
  poId: number;
  poNumber: string;
  supplierId?: number;
  rfqId?: number;
  status: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  totalAmount: number;
  currency: string;
  paymentTerms?: string;
  shippingAddress?: string;
  billingAddress?: string;
  notes?: string;
  createdBy?: string;
  createdAt: string;
  supplier?: SupplierDto;
  requestForQuote?: RfqDto;
  purchaseOrderLines: PurchaseOrderLineDto[];
}

export interface PurchaseOrderLineDto {
  poLineId: number;
  poId: number;
  quoteId?: number;
  lineNumber: number;
  itemId: number;
  quantityOrdered: number;
  unitPrice: number;
  totalPrice: number;
  deliveryDate?: string;
  description?: string;
  createdAt: string;
  quote?: QuoteDto;
  item: ItemDto;
}

export interface PurchaseOrderCreateDto {
  poNumber: string;
  supplierId?: number;
  rfqId?: number;
  status: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  totalAmount: number;
  currency: string;
  paymentTerms?: string;
  shippingAddress?: string;
  billingAddress?: string;
  notes?: string;
  createdBy?: string;
  purchaseOrderLines: PurchaseOrderLineCreateDto[];
}

export interface PurchaseOrderLineCreateDto {
  quoteId?: number;
  lineNumber: number;
  itemId: number;
  quantityOrdered: number;
  unitPrice: number;
  totalPrice: number;
  deliveryDate?: string;
  description?: string;
}

export interface PurchaseOrderUpdateDto {
  poNumber?: string;
  supplierId?: number;
  rfqId?: number;
  status?: string;
  orderDate?: string;
  expectedDeliveryDate?: string;
  totalAmount?: number;
  currency?: string;
  paymentTerms?: string;
  shippingAddress?: string;
  billingAddress?: string;
  notes?: string;
}

export interface PurchaseOrderSummaryDto {
  poId: number;
  poNumber: string;
  supplierName?: string;
  status: string;
  orderDate: string;
  totalAmount: number;
  currency: string;
  lineItemsCount: number;
}
