# Search Integration Plan - Chat Completion & Semantic Search

## Overview

This document outlines the plan to integrate chat completion API and semantic search functionality into the Angular frontend, following established patterns and leveraging the latest Angular 20 features.

## Current Architecture Analysis

### Backend API Endpoints
- `/api/Chat/completions` - Non-streaming chat completion
- `/api/Chat/completions/stream` - **Server-Sent Events (SSE) streaming chat completion** ⭐
- `/api/Search/semantic` - Semantic document search
- `/api/Search/documents` - Document metadata listing (TODO in backend)
- `/api/Documents/upload` - Document upload (future phase)

### Frontend Architecture
- **Angular 20.0.0** with latest features
- **Standalone components** and **signals** for state management
- Feature-based architecture (`/features/{feature}/`)
- Consistent service patterns using `ApiService`
- TailwindCSS for styling
- Lazy-loaded route modules

## Implementation Plan

### Phase 1: Core Infrastructure

#### 1.1 Search Service Implementation
**Location**: `/app/src/app/features/search/services/search.service.ts`

```typescript
@Injectable({
  providedIn: 'root'
})
export class SearchService {
  // Standard HTTP requests for semantic search
  // Server-Sent Events implementation for streaming chat
  // Signal-based state management
}
```

**Key Features**:
- **SSE streaming** for `/api/Chat/completions/stream`
- Semantic search integration
- **Angular signals** for reactive state management
- Error handling and retry logic
- Cancellation support for long-running requests

#### 1.2 Search Models
**Location**: `/app/src/app/shared/models/search.model.ts`

```typescript
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

export interface DocumentMetadata {
  id: string;
  fileName: string;
  uploadedAt: Date;
  chunkCount: number;
  status: string;
}
```

### Phase 2: Search Page Components

#### 2.1 Search Layout Component
**Location**: `/app/src/app/features/search/components/search-layout.component.ts`

**Features**:
- Modern split-pane layout
- Chat interface on the left
- Search results on the right
- Responsive design with TailwindCSS

#### 2.2 Chat Interface Component
**Location**: `/app/src/app/features/search/components/chat-interface.component.ts`

**Features**:
- **Real-time streaming** message display using SSE
- Message history with signals
- Auto-scroll to latest messages
- Typing indicators
- **Modern UI** with message bubbles
- Copy message functionality

#### 2.3 Search Results Component
**Location**: `/app/src/app/features/search/components/search-results.component.ts`

**Features**:
- Semantic search results display
- Document filtering
- Result relevance scoring
- Pagination support
- Citation extraction and display

#### 2.4 Document Browser Component
**Location**: `/app/src/app/features/search/components/document-browser.component.ts`

**Features**:
- Document metadata listing
- Search filtering by document
- Upload progress tracking (future)

### Phase 3: Advanced Features

#### 3.1 Server-Sent Events Implementation

```typescript
// Streaming chat with SSE
streamChat(message: string): Observable<StreamingChatChunk> {
  return new Observable(observer => {
    const eventSource = new EventSource('/api/Chat/completions/stream', {
      // Configuration for SSE
    });

    eventSource.onmessage = (event) => {
      // Handle streaming chunks
    };

    eventSource.onerror = (error) => {
      // Error handling
    };

    return () => eventSource.close();
  });
}
```

#### 3.2 Signal-Based State Management

```typescript
// Following established pattern from other features
export class SearchStore {
  // Chat state
  private _messages = signal<ChatMessage[]>([]);
  private _isStreaming = signal<boolean>(false);
  
  // Search state  
  private _searchResults = signal<SearchResult[]>([]);
  private _isSearching = signal<boolean>(false);
  
  // Computed signals
  readonly messages = this._messages.asReadonly();
  readonly hasMessages = computed(() => this._messages().length > 0);
}
```

### Phase 4: Routing & Navigation

#### 4.1 Search Routes
**Location**: `/app/src/app/features/search/search.routes.ts`

```typescript
export const searchRoutes: Routes = [
  {
    path: '',
    component: SearchLayoutComponent,
    children: [
      {
        path: '',
        component: SearchInterfaceComponent
      },
      {
        path: 'documents',
        component: DocumentBrowserComponent
      }
    ]
  }
];
```

#### 4.2 Main App Routes Update
**Location**: `/app/src/app/app.routes.ts`

```typescript
// Add search route to main routes
{
  path: 'search',
  loadChildren: () => import('./features/search/search.routes').then(m => m.searchRoutes)
}
```

## Technical Specifications

### Angular 20 Features Leveraged

1. **Signals for State Management**
   - Reactive state with automatic change detection
   - Computed values for derived state
   - Better performance than traditional RxJS patterns

2. **Standalone Components**
   - Tree-shakable and modular
   - Simplified dependency injection
   - Better lazy loading

3. **Control Flow Syntax**
   - `@if`, `@for`, `@switch` for templates
   - Better performance than `*ngIf`, `*ngFor`

4. **Resource API (Preview)**
   - For handling async operations
   - Perfect for search and streaming operations

### Server-Sent Events Implementation

```typescript
// Modern SSE with error handling and cleanup
private setupEventSource(request: ChatRequest): Observable<StreamingChatChunk> {
  return new Observable(observer => {
    const controller = new AbortController();
    
    fetch('/api/Chat/completions/stream', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'text/event-stream',
      },
      body: JSON.stringify(request),
      signal: controller.signal
    }).then(response => {
      const reader = response.body?.getReader();
      const decoder = new TextDecoder();
      
      const readChunk = () => {
        reader?.read().then(({ done, value }) => {
          if (done) {
            observer.complete();
            return;
          }
          
          const chunk = decoder.decode(value);
          const lines = chunk.split('\n');
          
          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const data = line.slice(6);
              if (data === '[DONE]') {
                observer.complete();
                return;
              }
              
              try {
                const parsed = JSON.parse(data);
                observer.next(parsed);
              } catch (e) {
                // Handle parsing errors
              }
            }
          }
          
          readChunk();
        });
      };
      
      readChunk();
    }).catch(error => {
      observer.error(error);
    });
    
    return () => controller.abort();
  });
}
```

## File Structure

```
app/src/app/features/search/
├── components/
│   ├── search-layout.component.ts
│   ├── search-layout.component.html
│   ├── search-layout.component.css
│   ├── chat-interface.component.ts
│   ├── chat-interface.component.html
│   ├── chat-interface.component.css
│   ├── search-results.component.ts
│   ├── search-results.component.html
│   ├── search-results.component.css
│   ├── document-browser.component.ts
│   ├── document-browser.component.html
│   └── document-browser.component.css
├── services/
│   ├── search.service.ts
│   └── search-store.service.ts
└── search.routes.ts

app/src/app/shared/models/
└── search.model.ts (new)
```

## UI/UX Design Principles

### Modern Chat Interface
- **Streaming messages** with smooth animations
- Message bubbles with user/assistant styling
- Auto-scroll with manual override
- Message timestamps and status indicators
- **Copy to clipboard** functionality

### Search Results
- **Relevance scoring** visualization
- Document source highlighting
- **Citation linking** back to original content
- Expandable result previews
- Filter and sorting options

### Responsive Design
- **Desktop**: Side-by-side chat and search
- **Mobile**: Tabbed interface
- Touch-friendly controls
- Proper accessibility support

## Development Workflow

### Phase 1: Core Service (Week 1)
1. Create search service with basic HTTP methods
2. Implement SSE streaming for chat completions
3. Add search models to shared models
4. Write unit tests for service methods

### Phase 2: Basic Components (Week 1-2)
1. Create search layout component
2. Implement basic chat interface
3. Add search results display
4. Setup routing and navigation

### Phase 3: Advanced Features (Week 2-3)
1. Enhance streaming with proper error handling
2. Add signal-based state management
3. Implement document filtering
4. Add citation extraction

### Phase 4: Polish & Testing (Week 3)
1. UI/UX improvements
2. Comprehensive testing
3. Performance optimization
4. Accessibility compliance

## Future Enhancements

### Document Upload Integration
- File upload component
- Progress tracking
- Document management interface
- Integration with `/api/Documents/upload`

### Advanced Search Features
- Search suggestions and autocomplete
- Search history
- Saved searches
- Advanced filtering options

### Chat Enhancements
- Conversation persistence
- Message editing
- Chat history export
- Multi-conversation support

## Testing Strategy

### Unit Tests
- Service methods with mocked HTTP calls
- SSE streaming with test event sources
- Signal state management
- Component logic testing

### Integration Tests
- End-to-end search workflow
- Chat streaming functionality
- Error handling scenarios
- Performance benchmarks

### E2E Tests
- Complete user journeys
- Cross-browser compatibility
- Mobile responsiveness
- Accessibility compliance

## Deployment Considerations

### Production Readiness
- Environment configuration for API endpoints
- Error monitoring and logging
- Performance metrics collection
- SEO optimization for search pages

### Security
- Input sanitization for chat messages
- XSS protection for search results
- Rate limiting considerations
- Content Security Policy updates

---

## Conclusion

This plan provides a comprehensive roadmap for integrating chat completion and semantic search functionality into the Angular frontend. By leveraging Angular 20's latest features and following established architectural patterns, we'll deliver a modern, performant, and maintainable search experience that can scale with future requirements.

The phased approach ensures steady progress while maintaining code quality and user experience throughout development.
