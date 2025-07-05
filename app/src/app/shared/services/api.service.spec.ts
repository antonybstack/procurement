import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiService } from './api.service';

describe('ApiService', () => {
    let service: ApiService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [
                ApiService,
                provideZonelessChangeDetection()
            ]
        });
        service = TestBed.inject(ApiService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should make GET request with params', () => {
        const testData = { id: 1, name: 'Test' };
        const params = { page: 1, size: 10 };

        service.get('test', params).subscribe(data => {
            expect(data).toEqual(testData);
        });

        const req = httpMock.expectOne('http://localhost:5001/api/test?page=1&size=10');
        expect(req.request.method).toBe('GET');
        req.flush(testData);
    });

    it('should make GET request without params', () => {
        const testData = { id: 1, name: 'Test' };

        service.get('test').subscribe(data => {
            expect(data).toEqual(testData);
        });

        const req = httpMock.expectOne('http://localhost:5001/api/test');
        expect(req.request.method).toBe('GET');
        req.flush(testData);
    });

    it('should make GET request by ID', () => {
        const testData = { id: 1, name: 'Test' };

        service.getById('test', 1).subscribe(data => {
            expect(data).toEqual(testData);
        });

        const req = httpMock.expectOne('http://localhost:5001/api/test/1');
        expect(req.request.method).toBe('GET');
        req.flush(testData);
    });

    it('should make POST request', () => {
        const testData = { id: 1, name: 'Test' };
        const postData = { name: 'Test' };

        service.post('test', postData).subscribe(data => {
            expect(data).toEqual(testData);
        });

        const req = httpMock.expectOne('http://localhost:5001/api/test');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(postData);
        req.flush(testData);
    });

    it('should make PUT request', () => {
        const testData = { id: 1, name: 'Updated Test' };
        const putData = { name: 'Updated Test' };

        service.put('test', 1, putData).subscribe(data => {
            expect(data).toEqual(testData);
        });

        const req = httpMock.expectOne('http://localhost:5001/api/test/1');
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(putData);
        req.flush(testData);
    });

    it('should make DELETE request', () => {
        service.delete('test', 1).subscribe();

        const req = httpMock.expectOne('http://localhost:5001/api/test/1');
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should handle null/undefined params', () => {
        const testData = { id: 1, name: 'Test' };
        const params = { page: 1, search: null, filter: undefined };

        service.get('test', params).subscribe(data => {
            expect(data).toEqual(testData);
        });

        const req = httpMock.expectOne('http://localhost:5001/api/test?page=1');
        expect(req.request.method).toBe('GET');
        req.flush(testData);
    });
}); 