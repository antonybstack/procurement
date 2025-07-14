export interface SupplierRecommendationDto {
    supplierId: number;
    supplierCode: string;
    companyName: string;
    country?: string;
    rating?: number;
    averagePrice?: number;
    quoteCount: number;
    awardedCount: number;
    successRate?: number;
    reasoning: string;
    confidenceScore: number;
    strengths: string[];
    considerations: string[];
}

export interface SupplierPerformanceAnalysisDto {
    itemCode: string;
    itemDescription: string;
    totalSuppliers: number;
    activeSuppliers: number;
    averagePrice: number;
    minPrice: number;
    maxPrice: number;
    priceVariance: number;
    topPerformers: SupplierRecommendationDto[];
    mostCompetitive: SupplierRecommendationDto[];
    marketInsights: string;
    recommendations: string[];
}
