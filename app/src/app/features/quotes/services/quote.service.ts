import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/services/api.service';
import { QuoteDto, QuoteSummaryDto, QuoteCreateDto, QuoteUpdateDto } from '../../../shared/models/quote.model';
import { PaginatedResult } from '../../../shared/models/rfq.model';

export interface QuoteFilters {
    page?: number;
    pageSize?: number;
    search?: string;
    status?: string;
    rfqId?: number;
    supplierId?: number;
    fromDate?: string;
    toDate?: string;
}

@Injectable({
    providedIn: 'root'
})
export class QuoteService {
    constructor(private apiService: ApiService) { }

    getQuotes(filters: QuoteFilters = {}): Observable<PaginatedResult<QuoteSummaryDto>> {
        return this.apiService.get<PaginatedResult<QuoteSummaryDto>>('quotes', filters);
    }

    getQuoteById(id: number): Observable<QuoteDto> {
        return this.apiService.getById<QuoteDto>('quotes', id);
    }

    createQuote(quoteData: QuoteCreateDto): Observable<QuoteDto> {
        return this.apiService.post<QuoteDto>('quotes', quoteData);
    }

    updateQuote(id: number, updateData: QuoteUpdateDto): Observable<QuoteDto> {
        return this.apiService.put<QuoteDto>('quotes', id, updateData);
    }

    deleteQuote(id: number): Observable<void> {
        return this.apiService.delete('quotes', id);
    }

    getStatuses(): Observable<string[]> {
        return this.apiService.get<string[]>('quotes/statuses');
    }

    getQuotesByRfq(rfqId: number): Observable<QuoteSummaryDto[]> {
        return this.apiService.get<QuoteSummaryDto[]>(`quotes/rfq/${rfqId}`);
    }

    getQuotesBySupplier(supplierId: number, page: number = 1, pageSize: number = 20): Observable<PaginatedResult<QuoteSummaryDto>> {
        return this.apiService.get<PaginatedResult<QuoteSummaryDto>>(`quotes/supplier/${supplierId}`, { page, pageSize });
    }

    getQuoteSummary(): Observable<any> {
        return this.apiService.get<any>('quotes/summary');
    }
} 