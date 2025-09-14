import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, BehaviorSubject, fromEvent, EMPTY } from 'rxjs';
import { map, catchError, takeUntil, finalize } from 'rxjs/operators';
import {
    ChatRequest,
    ChatResponse,
    StreamingChatChunk,
    ChatMessage,
    SemanticSearchRequest,
    SemanticSearchResponse,
    DocumentMetadata,
    SearchState,
    ChatState,
    SearchPageState
} from '../../../shared/models';

@Injectable({
    providedIn: 'root'
})
export class SearchService {
    private http = inject(HttpClient);
    private baseUrl = '/api';

    // Session management
    private _currentSessionId = signal<string | null>(null);

    // Signals for reactive state management
    private _chatState = signal<ChatState>({
        messages: [],
        isStreaming: false,
        currentStreamingMessageId: undefined,
        error: undefined
    });

    private _searchState = signal<SearchState>({
        isSearching: false,
        searchResults: [],
        searchQuery: '',
        selectedDocument: undefined,
        error: undefined
    });

    private _documents = signal<DocumentMetadata[]>([]);
    private _isLoadingDocuments = signal<boolean>(false);

    // Public readonly signals
    readonly chatState = this._chatState.asReadonly();
    readonly searchState = this._searchState.asReadonly();
    readonly documents = this._documents.asReadonly();
    readonly isLoadingDocuments = this._isLoadingDocuments.asReadonly();
    readonly currentSessionId = this._currentSessionId.asReadonly();

    // Computed signals
    readonly hasMessages = computed(() => this._chatState().messages.length > 0);
    readonly hasSearchResults = computed(() => this._searchState().searchResults.length > 0);
    readonly isActive = computed(() => this._chatState().isStreaming || this._searchState().isSearching);

    // Combined state for components that need full context
    readonly pageState = computed<SearchPageState>(() => ({
        chat: this._chatState(),
        search: this._searchState(),
        documents: this._documents(),
        isLoadingDocuments: this._isLoadingDocuments()
    }));

    // Subject for cancelling ongoing streams
    private cancelStream$ = new Subject<void>();

    /**
     * Send a chat message with streaming response using Server-Sent Events
     */
    streamChatCompletion(message: string): Observable<ChatMessage> {
        if (!message.trim()) {
            throw new Error('Message cannot be empty');
        }

        // Add user message immediately
        const userMessage: ChatMessage = {
            id: this.generateId(),
            content: message,
            role: 'user',
            timestamp: new Date()
        };

        this.addMessage(userMessage);

        // Create assistant message for streaming
        const assistantMessage: ChatMessage = {
            id: this.generateId(),
            content: '',
            role: 'assistant',
            timestamp: new Date(),
            isStreaming: true
        };

        this.addMessage(assistantMessage);
        this.setStreaming(true, assistantMessage.id);

        const request: ChatRequest = { message };

        return this.setupEventSource(request).pipe(
            map(chunk => {
                // Update the assistant message with streaming content
                this.updateStreamingMessage(assistantMessage.id, chunk.content);
                return assistantMessage;
            }),
            catchError((error: Error) => {
                this.handleStreamError(assistantMessage.id, error);
                return EMPTY;
            }),
            finalize(() => {
                this.setStreaming(false);
                this.finalizeStreamingMessage(assistantMessage.id);
            }),
            takeUntil(this.cancelStream$)
        );
    }

    /**
     * Send a non-streaming chat completion request
     */
    sendChatCompletion(message: string): Observable<ChatResponse> {
        const request: ChatRequest = { message };
        return this.http.post<ChatResponse>(`${this.baseUrl}/Chat/completions`, request)
            .pipe(
                catchError(this.handleHttpError<ChatResponse>('sendChatCompletion'))
            );
    }

    /**
     * Perform semantic search across documents
     */
    searchDocuments(query: string, documentFilter?: string, maxResults?: number): Observable<SemanticSearchResponse> {
        this.setSearching(true, query);

        const request: SemanticSearchRequest = {
            query,
            documentFilter,
            maxResults: maxResults || 5
        };

        return this.http.post<SemanticSearchResponse>(`${this.baseUrl}/Search/semantic`, request)
            .pipe(
                map(response => {
                    this.updateSearchResults(response);
                    return response;
                }),
                catchError((error: HttpErrorResponse) => {
                    this.setSearchError(error.message);
                    return EMPTY;
                }),
                finalize(() => this.setSearching(false))
            );
    }

    /**
     * Get list of available documents
     */
    loadDocuments(): Observable<DocumentMetadata[]> {
        this._isLoadingDocuments.set(true);

        return this.http.get<{ documents: DocumentMetadata[] }>(`${this.baseUrl}/Search/documents`)
            .pipe(
                map(response => response.documents),
                map(documents => {
                    this._documents.set(documents);
                    return documents;
                }),
                catchError(this.handleHttpError<DocumentMetadata[]>('loadDocuments')),
                finalize(() => this._isLoadingDocuments.set(false))
            );
    }

    /**
     * Cancel any ongoing streaming operation
     */
    cancelStream(): void {
        this.cancelStream$.next();
        this.setStreaming(false);
    }

    /**
     * Clear chat messages and reset session
     */
    clearChat(): void {
        this._chatState.update(state => ({
            ...state,
            messages: [],
            error: undefined
        }));
        this._currentSessionId.set(null);
    }

    /**
     * Clear search results
     */
    clearSearch(): void {
        this._searchState.update(state => ({
            ...state,
            searchResults: [],
            searchQuery: '',
            selectedDocument: undefined,
            error: undefined
        }));
    }

    /**
     * Set selected document filter
     */
    setDocumentFilter(documentId?: string): void {
        this._searchState.update(state => ({
            ...state,
            selectedDocument: documentId
        }));
    }

    // Private helper methods

    private setupEventSource(request: ChatRequest): Observable<StreamingChatChunk> {
        return new Observable(observer => {
            const controller = new AbortController();

            // Prepare headers including session management
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
                'Accept': 'text/event-stream',
                'Cache-Control': 'no-cache'
            };

            // Add session headers - only if we already have messages in this session
            const currentSessionId = this._currentSessionId();
            const hasMessages = this._chatState().messages.length > 0;
            if (currentSessionId && hasMessages) {
                headers['X-Chat-Session-Id'] = currentSessionId;
            }

            // Add user ID header (you can customize this logic)
            const userId = this.getCurrentUserId();
            if (userId) {
                headers['X-User-Id'] = userId;
            }

            fetch(`${this.baseUrl}/Chat/completions/stream`, {
                method: 'POST',
                headers,
                body: JSON.stringify(request),
                signal: controller.signal
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                    }

                    // Handle session ID from response headers
                    const responseSessionId = response.headers.get('X-Chat-Session-Id');
                    if (responseSessionId) {
                        this.setSessionId(responseSessionId);
                    }

                    if (!response.body) {
                        throw new Error('Response body is null');
                    }

                    const reader = response.body.getReader();
                    const decoder = new TextDecoder();

                    const readChunk = (): void => {
                        reader.read().then(({ done, value }) => {
                            if (done) {
                                observer.complete();
                                return;
                            }

                            const chunk = decoder.decode(value, { stream: true });
                            const lines = chunk.split('\n');

                            for (const line of lines) {
                                if (line.startsWith('data: ')) {
                                    const data = line.slice(6).trim();

                                    if (data === '[DONE]') {
                                        observer.complete();
                                        return;
                                    }

                                    try {
                                        const parsed: StreamingChatChunk = JSON.parse(data);
                                        observer.next(parsed);
                                    } catch (e) {
                                        console.warn('Failed to parse SSE data:', data, e);
                                    }
                                }
                            }

                            readChunk();
                        }).catch(error => {
                            observer.error(error);
                        });
                    };

                    readChunk();
                })
                .catch(error => {
                    observer.error(error);
                });

            return () => {
                controller.abort();
            };
        });
    }

    private addMessage(message: ChatMessage): void {
        this._chatState.update(state => ({
            ...state,
            messages: [...state.messages, message],
            error: undefined
        }));
    }

    private updateStreamingMessage(messageId: string, newContent: string): void {
        this._chatState.update(state => ({
            ...state,
            messages: state.messages.map(msg =>
                msg.id === messageId
                    ? { ...msg, content: msg.content + newContent }
                    : msg
            )
        }));
    }

    private finalizeStreamingMessage(messageId: string): void {
        this._chatState.update(state => ({
            ...state,
            messages: state.messages.map(msg =>
                msg.id === messageId
                    ? { ...msg, isStreaming: false }
                    : msg
            )
        }));
    }

    private setStreaming(isStreaming: boolean, messageId?: string): void {
        this._chatState.update(state => ({
            ...state,
            isStreaming,
            currentStreamingMessageId: isStreaming ? messageId : undefined
        }));
    }

    private handleStreamError(messageId: string, error: Error): void {
        // Update the streaming message to show error
        this._chatState.update(state => ({
            ...state,
            messages: state.messages.map(msg =>
                msg.id === messageId
                    ? { ...msg, content: msg.content || 'Error occurred while streaming response', isStreaming: false }
                    : msg
            ),
            isStreaming: false,
            currentStreamingMessageId: undefined,
            error: error.message
        }));
    }

    private setSearching(isSearching: boolean, query?: string): void {
        this._searchState.update(state => ({
            ...state,
            isSearching,
            searchQuery: query || state.searchQuery,
            error: undefined
        }));
    }

    private updateSearchResults(response: SemanticSearchResponse): void {
        this._searchState.update(state => ({
            ...state,
            searchResults: response.results,
            searchQuery: response.query
        }));
    }

    private setSearchError(error: string): void {
        this._searchState.update(state => ({
            ...state,
            error,
            isSearching: false
        }));
    }

    private generateId(): string {
        return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    private handleHttpError<T>(operation = 'operation') {
        return (error: HttpErrorResponse): Observable<T> => {
            console.error(`${operation} failed:`, error);

            let errorMessage = 'An error occurred';
            if (error.error?.message) {
                errorMessage = error.error.message;
            } else if (error.message) {
                errorMessage = error.message;
            }

            // Let the app keep running by returning an empty result
            return EMPTY;
        };
    }

    // Session management helper methods

    private setSessionId(sessionId: string): void {
        this._currentSessionId.set(sessionId);
    }

    private getCurrentUserId(): string | null {
        // For now, return a default user ID
        // In a real application, this would come from authentication service
        return 'frontend-user';
    }

    // Public session methods

    /**
     * Get the current session ID
     */
    getSessionId(): string | null {
        return this._currentSessionId();
    }

    /**
     * Force set a session ID (useful for testing or session restoration)
     */
    setCurrentSessionId(sessionId: string | null): void {
        this._currentSessionId.set(sessionId);
    }
}
