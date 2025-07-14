import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../shared/services/api.service';
import { SupplierPerformanceAnalysisDto } from '../../../shared/models/ai-analysis.model';

@Injectable({
    providedIn: 'root',
})
export class AiService {
    private apiService = inject(ApiService);

    getPerformanceAnalysis(itemId: number): Observable<SupplierPerformanceAnalysisDto> {
        return this.apiService.get<SupplierPerformanceAnalysisDto>(`airecommendations/analysis/${itemId}`);
    }
}
