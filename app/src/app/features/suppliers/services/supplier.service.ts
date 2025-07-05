import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/services/api.service';
import { SupplierDto } from '../../../shared/models/supplier.model';
import { PaginatedResult } from '../../../shared/models/rfq.model';

export interface SupplierFilters {
    page?: number;
    pageSize?: number;
    search?: string;
    country?: string;
    minRating?: number;
    isActive?: boolean;
}

export interface SupplierUpdateDto {
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
}

@Injectable({
    providedIn: 'root'
})
export class SupplierService {
    constructor(private apiService: ApiService) { }

    getSuppliers(filters: SupplierFilters = {}): Observable<PaginatedResult<SupplierDto>> {
        return this.apiService.get<PaginatedResult<SupplierDto>>('suppliers', filters);
    }

    getSupplierById(id: number): Observable<SupplierDto> {
        return this.apiService.getById<SupplierDto>('suppliers', id);
    }

    updateSupplier(id: number, updateData: SupplierUpdateDto): Observable<SupplierDto> {
        return this.apiService.put<SupplierDto>('suppliers', id, updateData);
    }

    getCountries(): Observable<string[]> {
        return this.apiService.get<string[]>('suppliers/countries');
    }
} 