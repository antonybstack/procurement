import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/services/api.service';
import { Item } from '../../../shared/models/item.model';
import { PaginatedResult } from '../../../shared/models/rfq.model';

export interface ItemFilters {
    page?: number;
    pageSize?: number;
    search?: string;
    category?: string;
}

@Injectable({
    providedIn: 'root'
})
export class ItemService {
    constructor(private apiService: ApiService) { }

    getItems(filters: ItemFilters = {}): Observable<PaginatedResult<Item>> {
        return this.apiService.get<PaginatedResult<Item>>('items', filters);
    }

    getItemById(id: number): Observable<Item> {
        return this.apiService.getById<Item>('items', id);
    }

    createItem(item: Partial<Item>): Observable<Item> {
        return this.apiService.post<Item>('items', item);
    }

    updateItem(id: number, item: Partial<Item>): Observable<Item> {
        return this.apiService.put<Item>('items', id, item);
    }

    deleteItem(id: number): Observable<void> {
        return this.apiService.delete('items', id);
    }

    getCategories(): Observable<string[]> {
        return this.apiService.get<string[]>('items/categories');
    }

    getItemSummary(): Observable<any> {
        return this.apiService.get<any>('items/summary');
    }
} 