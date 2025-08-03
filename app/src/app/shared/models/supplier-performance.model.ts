import { SupplierDto } from "./supplier.model";

export interface SupplierPerformanceDataDto {
  totalQuotes: number;
  awardedQuotes: number;
  avgQuotePrice: number | null;
  totalPurchaseOrders: number;
}

export interface SupplierDetailDto extends SupplierDto {
  performance: SupplierPerformanceDataDto | null;
}
