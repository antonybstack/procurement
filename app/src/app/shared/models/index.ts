// This file re-exports all the interfaces from the other model files.
// This is a "barrel" file, which makes it easier to import models from other parts of the application.

// AI-related models
export type {
  SupplierRecommendationDto,
  SupplierPerformanceAnalysisDto,
  AiRecommendationRequestDto,
  AiRecommendationResponseDto,
  SupplierCapabilityAnalysisDto,
} from './ai-analysis.model';

// Core entity models
export type { ItemDto, ItemCreateDto, ItemUpdateDto, ItemSummary, ItemSummaryResponse } from './item.model';
export type { QuoteDto, QuoteSummaryDto, QuoteCreateDto, QuoteUpdateDto } from './quote.model';
export type {
  PurchaseOrderDto,
  PurchaseOrderLineDto,
  PurchaseOrderCreateDto,
  PurchaseOrderLineCreateDto,
  PurchaseOrderUpdateDto,
  PurchaseOrderSummaryDto,
} from './purchase-order.model';
export type { RfqDto, RfqLineItemDto, RfqDetailDto, PaginatedResult } from './rfq.model';

// Search and chat models
export type {
  ChatRequest,
  ChatResponse,
  StreamingChatChunk,
  ChatMessage,
  SemanticSearchRequest,
  SearchResult,
  SemanticSearchResponse,
  DocumentMetadata,
  SearchState,
  ChatState,
  SearchPageState
} from './search.model';

export type { SupplierDto, SupplierSummaryDto, SupplierCapabilityDto } from './supplier.model';
export type { SupplierDetailDto, SupplierPerformanceDataDto } from './supplier-performance.model';

