export interface Item {
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

export interface ItemCreateDto {
    itemCode: string;
    description: string;
    category: string;
    unitOfMeasure: string;
    standardCost?: number;
    minOrderQuantity: number;
    leadTimeDays: number;
    isActive: boolean;
}

export interface ItemUpdateDto {
    itemCode: string;
    description: string;
    category: string;
    unitOfMeasure: string;
    standardCost?: number;
    minOrderQuantity: number;
    leadTimeDays: number;
    isActive: boolean;
}

export interface ItemSummary {
    category: string;
    count: number;
    activeCount: number;
    avgStandardCost: number;
}

export interface ItemSummaryResponse {
    summary: ItemSummary[];
    totalItems: number;
    activeItems: number;
    averageLeadTime: number;
} 