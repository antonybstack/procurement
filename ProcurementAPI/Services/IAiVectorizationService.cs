using ProcurementAPI.Models;

namespace ProcurementAPI.Services;

/// <summary>
/// Service for handling AI vectorization operations
/// </summary>
public interface IAiVectorizationService
{
    /// <summary>
    /// Generates embeddings for all suppliers in the database
    /// </summary>
    Task<int> VectorizeAllSuppliersAsync(CancellationToken ct);

    /// <summary>
    /// Generates embeddings for all items in the database
    /// </summary>
    Task<int> VectorizeAllItemsAsync();

    /// <summary>
    /// Generates embedding for a single supplier
    /// </summary>
    Task<float[]?> GenerateSupplierEmbeddingAsync(Supplier supplier);

    /// <summary>
    /// Generates embedding for a single item
    /// </summary>
    Task<float[]?> GenerateItemEmbeddingAsync(Item item);

    /// <summary>
    /// Generates embedding for a single RFQ
    /// </summary>
    Task<float[]?> GenerateRfqEmbeddingAsync(RequestForQuote rfq);

    /// <summary>
    /// Generates embedding for a single quote
    /// </summary>
    Task<float[]?> GenerateQuoteEmbeddingAsync(Quote quote);

    /// <summary>
    /// Generates embedding for a text query
    /// </summary>
    Task<float[]?> GenerateQueryEmbeddingAsync(string query);

    /// <summary>
    /// Finds similar suppliers based on a query
    /// </summary>
    Task<List<Supplier>> FindSimilarSuppliersAsync(string query, int limit = 10);

    /// <summary>
    /// Finds similar items based on a query
    /// </summary>
    Task<List<Item>> FindSimilarItemsAsync(string query, int limit = 10);

    /// <summary>
    /// Performs semantic search across all vectorized entities
    /// </summary>
    Task<SemanticSearchResult> PerformSemanticSearchAsync(string query, int limit = 20);
}

/// <summary>
/// Result of semantic search across multiple entity types
/// </summary>
public class SemanticSearchResult
{
    public List<Supplier> Suppliers { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<RequestForQuote> RequestForQuotes { get; set; } = new();
    public List<Quote> Quotes { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
}

/// <summary>
/// Result of semantic search using vector store models
/// </summary>
public class VectorStoreSearchResult
{
    public List<SupplierVectorModel> Suppliers { get; set; } = new();
    public List<ItemVectorModel> Items { get; set; } = new();
    public List<RfqVectorModel> RequestForQuotes { get; set; } = new();
    public List<QuoteVectorModel> Quotes { get; set; } = new();
}