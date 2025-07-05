export interface SupplierDto {
    supplierId: number;
    supplierCode: string;
    companyName: string;
    contactName?: string;
    email?: string;
    phone?: string;
    address?: string;
    city?: string;
    state?: string;
    country?: string;
    postalCode?: string;
    taxId?: string;
    paymentTerms?: string;
    creditLimit?: number;
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