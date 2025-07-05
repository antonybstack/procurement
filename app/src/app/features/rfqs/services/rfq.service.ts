import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/services/api.service';
import { RfqDto, RfqDetailDto, PaginatedResult } from '../../../shared/models/rfq.model';

export interface RfqFilters {
    page?: number;
    pageSize?: number;
    search?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
}

@Injectable({
    providedIn: 'root'
})
export class RfqService {
    constructor(private apiService: ApiService) { }

    getRfqs(filters: RfqFilters = {}): Observable<PaginatedResult<RfqDto>> {
        return this.apiService.get<PaginatedResult<RfqDto>>('rfqs', filters);
    }

    getRfqById(id: number): Observable<RfqDetailDto> {
        return this.apiService.getById<RfqDetailDto>('rfqs', id);
    }

    getRfqStatuses(): Observable<string[]> {
        return this.apiService.get<string[]>('rfqs/statuses');
    }

    getRfqSummary(): Observable<any> {
        return this.apiService.get<any>('rfqs/summary');
    }
} 