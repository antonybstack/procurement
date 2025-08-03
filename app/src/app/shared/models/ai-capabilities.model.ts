export interface SupplierCapabilityDto {
    capabilityId: number;
    supplierId: number;
    capabilityType: string;
    capabilityValue: string;
    createdAt: string;
}

export interface ItemSpecificationDto {
    specId: number;
    itemId: number;
    specName: string;
    specValue: string;
    createdAt: string;
}

export interface SupplierCapabilityCreateDto {
    supplierId: number;
    capabilityType: string;
    capabilityValue: string;
}

export interface ItemSpecificationCreateDto {
    itemId: number;
    specName: string;
    specValue: string;
}

export interface SupplierCapabilityUpdateDto {
    capabilityType?: string;
    capabilityValue?: string;
}

export interface ItemSpecificationUpdateDto {
    specName?: string;
    specValue?: string;
}

export interface SupplierWithCapabilitiesDto {
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
    capabilities: SupplierCapabilityDto[];
}

export interface ItemWithSpecificationsDto {
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
    specifications: ItemSpecificationDto[];
}
