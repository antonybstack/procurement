import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/services/api.service';
import { ItemDto } from '../../../shared/models/item.model';
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

  getItems(filters: ItemFilters = {}): Observable<PaginatedResult<ItemDto>> {
    return this.apiService.get<PaginatedResult<ItemDto>>('items', filters);
  }

  getItemById(id: number): Observable<ItemDto> {
    return this.apiService.getById<ItemDto>('items', id);
  }

  createItem(item: Partial<ItemDto>): Observable<ItemDto> {
    return this.apiService.post<ItemDto>('items', item);
  }

  updateItem(id: number, item: Partial<ItemDto>): Observable<ItemDto> {
    return this.apiService.put<ItemDto>('items', id, item);
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
