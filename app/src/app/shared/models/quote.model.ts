import { PurchaseOrderLineDto } from '.';
import { SupplierDto } from './supplier.model';

export interface QuoteDto {
  quoteId: number;
  rfqId: number;
  supplierId: number;
  lineItemId: number;
  quoteNumber: string;
  status: string;
  unitPrice: number;
  totalPrice: number;
  quantityOffered: number;
  deliveryDate?: string;
  paymentTerms?: string;
  warrantyPeriodMonths?: number;
  technicalComplianceNotes?: string;
  submittedDate: string;
  validUntilDate?: string;
  createdAt: string;
  updatedAt?: string;
  supplier: SupplierDto;
  rfqLineItem: RfqLineItemDto;
  purchaseOrderLines?: PurchaseOrderLineDto[];
}

export interface QuoteSummaryDto {
  quoteId: number;
  quoteNumber: string;
  status: string;
  unitPrice: number;
  totalPrice: number;
  quantityOffered: number;
  deliveryDate?: string;
  submittedDate: string;
  supplierName: string;
  rfqNumber: string;
  itemDescription: string;
}

export interface QuoteCreateDto {
  rfqId: number;
  supplierId: number;
  lineItemId: number;
  quoteNumber: string;
  status: string;
  unitPrice: number;
  totalPrice: number;
  quantityOffered: number;
  deliveryDate?: string;
  paymentTerms?: string;
  warrantyPeriodMonths?: number;
  technicalComplianceNotes?: string;
  validUntilDate?: string;
}

export interface QuoteUpdateDto {
  quoteNumber: string;
  status: string;
  unitPrice: number;
  totalPrice: number;
  quantityOffered: number;
  deliveryDate?: string;
  paymentTerms?: string;
  warrantyPeriodMonths?: number;
  technicalComplianceNotes?: string;
  validUntilDate?: string;
}

export interface RfqLineItemDto {
  lineItemId: number;
  lineNumber: number;
  quantityRequired: number;
  unitOfMeasure: string;
  description?: string;
  deliveryDate?: string;
  estimatedUnitCost?: number;
  item?: ItemDto;
}

export interface ItemDto {
  itemId: number;
  itemCode: string;
  description: string;
  category: string;
  unitOfMeasure: string;
  standardCost?: number;
  minOrderQuantity: number;
  leadTimeDays: number;
  isActive: boolean;
  createdAt: string;
}
