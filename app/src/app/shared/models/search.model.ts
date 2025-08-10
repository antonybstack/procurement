// Chat completion models
export interface ChatRequest {
  message: string;
}

export interface ChatResponse {
  id: string;
  content: string;
  createdAt: Date;
}

export interface StreamingChatChunk {
  id: string;
  content: string;
  createdAt: Date;
}

export interface ChatMessage {
  id: string;
  content: string;
  role: 'user' | 'assistant';
  timestamp: Date;
  isStreaming?: boolean;
}

// Search models
export interface SemanticSearchRequest {
  query: string;
  documentFilter?: string;
  maxResults?: number;
}

export interface SearchResult {
  documentId: string;
  pageNumber: number;
  text: string;
  similarity: number;
}

export interface SemanticSearchResponse {
  results: SearchResult[];
  query: string;
}

export interface DocumentMetadata {
  id: string;
  fileName: string;
  uploadedAt: Date;
  chunkCount: number;
  status: string;
}

// State models for search store
export interface SearchState {
  isSearching: boolean;
  searchResults: SearchResult[];
  searchQuery: string;
  selectedDocument?: string;
  error?: string;
}

export interface ChatState {
  messages: ChatMessage[];
  isStreaming: boolean;
  currentStreamingMessageId?: string;
  error?: string;
}

// Combined search page state
export interface SearchPageState {
  chat: ChatState;
  search: SearchState;
  documents: DocumentMetadata[];
  isLoadingDocuments: boolean;
}
