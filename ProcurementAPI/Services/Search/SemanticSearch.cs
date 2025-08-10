using Microsoft.Extensions.VectorData;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services;

/// <summary>
/// Provides semantic search capabilities for ingested document chunks using vector similarity.
/// This service abstracts the underlying vector storage implementation and provides a clean interface
/// for performing semantic searches across document content.
/// </summary>
/// <remarks>
/// The SemanticSearch service uses dependency injection to receive a VectorStoreCollection configured
/// for IngestedChunk objects. The actual storage backend (SQLite vector database) is configured in
/// Program.cs using the collection name "data-chatapp-chunks".
/// 
/// The service leverages vector embeddings to find semantically similar content by:
/// 1. Converting the search text to vector embeddings (handled by the vector store)
/// 2. Computing cosine similarity against stored document chunk vectors
/// 3. Returning the most relevant chunks based on similarity scores
/// 
/// Key abstractions used:
/// - VectorStoreCollection: Core abstraction from Microsoft.Extensions.VectorData
/// - IngestedChunk: Data model with vector embeddings and metadata
/// - VectorSearchOptions: Configuration for search behavior and filtering
/// </remarks>
public class SemanticSearch(
    VectorStoreCollection<string, IngestedChunk> vectorCollection)
{
    /// <summary>
    /// Performs a semantic search across ingested document chunks using vector similarity.
    /// </summary>
    /// <param name="text">The search query text that will be converted to vector embeddings for similarity comparison</param>
    /// <param name="documentIdFilter">Optional document ID to restrict search results to a specific document. 
    /// If null or empty, searches across all documents.</param>
    /// <param name="maxResults">Maximum number of results to return, ordered by similarity score (most similar first)</param>
    /// <returns>
    /// A read-only list of IngestedChunk objects representing the most semantically similar content,
    /// sorted by relevance score in descending order
    /// </returns>
    /// <remarks>
    /// The search process:
    /// 1. The input text is automatically converted to vector embeddings by the vector store
    /// 2. Cosine similarity is computed against all stored chunk vectors (configured in IngestedChunk)
    /// 3. Results are filtered by documentIdFilter if provided
    /// 4. Top maxResults chunks are returned, ordered by similarity score
    /// 
    /// Performance considerations:
    /// - Vector similarity computation is optimized by the underlying SQLite vector database
    /// - Filtering by documentId uses an indexed field for efficient queries
    /// - Results are streamed and can be processed asynchronously
    /// </remarks>
    /// <example>
    /// <code>
    /// // Search across all documents
    /// var results = await semanticSearch.SearchAsync("machine learning concepts", null, 5);
    /// 
    /// // Search within a specific document
    /// var results = await semanticSearch.SearchAsync("neural networks", "doc-123", 10);
    /// </code>
    /// </example>
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        // Configure search options with optional document filtering
        var searchOptions = new VectorSearchOptions<IngestedChunk>
        {
            // Apply document filter only if a non-empty documentIdFilter is provided
            // The lambda expression filters records where DocumentId matches the filter
            Filter = documentIdFilter is { Length: > 0 }
                ? record => record.DocumentId == documentIdFilter
                : null,
        };

        // Perform vector similarity search asynchronously
        // The vector store automatically handles:
        // - Converting input text to vector embeddings
        // - Computing similarity scores using cosine distance
        // - Ranking results by similarity score
        var nearest = vectorCollection.SearchAsync(text, maxResults, searchOptions);

        // Extract the IngestedChunk records from search results
        // SearchAsync returns VectorSearchResult<TRecord> objects that contain both
        // the record and metadata like similarity scores
        return await nearest.Select(result => result.Record).ToListAsync();
    }
}
