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

    getCountries(): Observable<string[]> {
        return this.apiService.get<string[]>('suppliers/countries');
    }
} 