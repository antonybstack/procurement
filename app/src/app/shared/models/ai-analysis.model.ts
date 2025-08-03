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
    capabilities?: string[];
    similarityScore?: number;
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

export interface AiRecommendationRequestDto {
    query: string;
    itemId?: number;
    category?: string;
    specifications?: string[];
    maxResults?: number;
    includeCapabilities?: boolean;
    includeSimilarityScores?: boolean;
}

export interface AiRecommendationResponseDto {
    recommendations: SupplierRecommendationDto[];
    query: string;
    processingTime: number;
    totalSuppliersAnalyzed: number;
    confidenceThreshold: number;
    modelVersion: string;
    reasoning: string;
}

export interface SupplierCapabilityAnalysisDto {
    supplierId: number;
    companyName: string;
    capabilities: string[];
    matchingCapabilities: string[];
    missingCapabilities: string[];
    capabilityScore: number;
    recommendations: string[];
}
