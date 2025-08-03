import { ItemDto, QuoteDto } from '.';
import { SupplierDto } from './supplier.model';

export interface RfqDto {
  rfqId: number;
  rfqNumber: string;
  title: string;
  description?: string;
  status: string;
  issueDate: string;
  dueDate: string;
  awardDate?: string;
  totalEstimatedValue?: number;
  currency: string;
  createdBy?: string;
  createdAt: string;
  lineItemsCount: number;
  suppliersInvited: number;
  quotesReceived: number;
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

export interface RfqDetailDto extends RfqDto {
  lineItems: RfqLineItemDto[];
  invitedSuppliers: SupplierDto[];
  quotes: QuoteDto[];
}

export interface PaginatedResult<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
