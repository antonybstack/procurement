import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private http = inject(HttpClient);
    private baseUrl = 'http://localhost:5001/api';

    get<T>(endpoint: string, params?: Record<string, any>): Observable<T> {
        let httpParams = new HttpParams();
        if (params) {
            Object.keys(params).forEach(key => {
                if (params[key] !== null && params[key] !== undefined) {
                    httpParams = httpParams.set(key, params[key].toString());
                }
            });
        }
        return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { params: httpParams });
    }

    getById<T>(endpoint: string, id: number): Observable<T> {
        return this.http.get<T>(`${this.baseUrl}/${endpoint}/${id}`);
    }

    post<T>(endpoint: string, data: any): Observable<T> {
        return this.http.post<T>(`${this.baseUrl}/${endpoint}`, data);
    }

    put<T>(endpoint: string, id: number, data: any): Observable<T> {
        return this.http.put<T>(`${this.baseUrl}/${endpoint}/${id}`, data);
    }

    patch<T>(endpoint: string, id: number, data: any): Observable<T> {
        return this.http.patch<T>(`${this.baseUrl}/${endpoint}/${id}`, data);
    }

    delete<T>(endpoint: string, id: number): Observable<T> {
        return this.http.delete<T>(`${this.baseUrl}/${endpoint}/${id}`);
    }
} 