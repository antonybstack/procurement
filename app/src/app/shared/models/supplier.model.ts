export interface SupplierDto {
    supplierId: number;
    supplierCode: string;
    companyName: string;
    contactName?: string;
    email?: string;
    phone?: string;
    city?: string;
    state?: string;
    country?: string;
    rating?: number;
    isActive: boolean;
    createdAt: string;
}

export interface SupplierSummaryDto {
    supplierId: number;
    companyName: string;
    rating?: number;
    totalQuotes: number;
    awardedQuotes: number;
    avgQuotePrice?: number;
    totalPurchaseOrders: number;
    totalPoValue?: number;
} 