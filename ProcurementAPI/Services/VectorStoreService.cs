using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.PgVector;
using Microsoft.SemanticKernel.Memory;
using Npgsql;
using ProcurementAPI.Data;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services;

/// <summary>
/// Service for handling vector store operations using Semantic Kernel's Postgres connector
/// </summary>
public class VectorStoreService : IVectorStoreService
{
    private readonly ILogger<VectorStoreService> _logger;
    private readonly ProcurementDbContext _context;
    private readonly PostgresVectorStore _vectorStore;
    private readonly IAiVectorizationService _aiVectorizationService;

    public VectorStoreService(
        ILogger<VectorStoreService> logger,
        ProcurementDbContext context,
        PostgresVectorStore vectorStore,
        IAiVectorizationService aiVectorizationService)
    {
        _logger = logger;
        _context = context;
        _vectorStore = vectorStore;
        _aiVectorizationService = aiVectorizationService;
    }

    public async Task<int> VectorizeAllSuppliersAsync()
    {
        _logger.LogInformation("Starting vectorization of all suppliers using AI service");

        var suppliers = await _context.Suppliers
            .Include(s => s.SupplierCapabilities)
            .Where(s => s.IsActive)
            .ToListAsync();

        var count = 0;
        foreach (var supplier in suppliers)
        {
            try
            {
                // Generate embedding using AI service
                var embedding = await _aiVectorizationService.GenerateSupplierEmbeddingAsync(supplier);
                if (embedding != null && embedding.Length > 0)
                {
                    // Use raw SQL to update the embedding directly in the database
                    var embeddingString = string.Join(",", embedding);

                    // Use string interpolation to avoid parameterization issues with vector type
                    var sql = $"UPDATE suppliers SET embedding = '[{embeddingString}]'::vector WHERE supplier_id = {supplier.SupplierId}";
                    await _context.Database.ExecuteSqlRawAsync(sql);

                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to vectorize supplier {SupplierId}", supplier.SupplierId);
            }
        }

        _logger.LogInformation("Completed vectorization of {Count} suppliers", count);
        return count;
    }

    public async Task<int> VectorizeAllItemsAsync()
    {
        _logger.LogInformation("Starting vectorization of all items using AI service");

        var items = await _context.Items
            .Include(i => i.ItemSpecifications)
            .Where(i => i.IsActive)
            .ToListAsync();

        var count = 0;
        foreach (var item in items)
        {
            try
            {
                // Generate embedding using AI service
                var embedding = await _aiVectorizationService.GenerateItemEmbeddingAsync(item);
                if (embedding != null && embedding.Length > 0)
                {
                    // Use raw SQL to update the embedding directly in the database
                    var embeddingString = string.Join(",", embedding);

                    // Use string interpolation to avoid parameterization issues with vector type
                    var sql = $"UPDATE items SET embedding = '[{embeddingString}]'::vector WHERE item_id = {item.ItemId}";
                    await _context.Database.ExecuteSqlRawAsync(sql);

                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to vectorize item {ItemId}", item.ItemId);
            }
        }

        _logger.LogInformation("Completed vectorization of {Count} items", count);
        return count;
    }

    public async Task<int> VectorizeAllRfqsAsync()
    {
        _logger.LogInformation("Starting vectorization of all RFQs using AI service");

        var rfqs = await _context.RequestForQuotes
            .Include(rfq => rfq.RfqLineItems)
            .ThenInclude(rli => rli.Item)
            .ToListAsync();

        var count = 0;
        foreach (var rfq in rfqs)
        {
            try
            {
                // Generate embedding using AI service
                var embedding = await _aiVectorizationService.GenerateRfqEmbeddingAsync(rfq);
                if (embedding != null && embedding.Length > 0)
                {
                    // Use raw SQL to update the embedding directly in the database
                    var embeddingString = string.Join(",", embedding);

                    // Use string interpolation to avoid parameterization issues with vector type
                    var sql = $"UPDATE request_for_quotes SET embedding = '[{embeddingString}]'::vector WHERE rfq_id = {rfq.RfqId}";
                    await _context.Database.ExecuteSqlRawAsync(sql);

                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to vectorize RFQ {RfqId}", rfq.RfqId);
            }
        }

        _logger.LogInformation("Completed vectorization of {Count} RFQs", count);
        return count;
    }

    public async Task<int> VectorizeAllQuotesAsync()
    {
        _logger.LogInformation("Starting vectorization of all quotes using AI service");

        var quotes = await _context.Quotes
            .Include(q => q.Supplier)
            .ToListAsync();

        var count = 0;
        foreach (var quote in quotes)
        {
            try
            {
                // Generate embedding using AI service
                var embedding = await _aiVectorizationService.GenerateQuoteEmbeddingAsync(quote);
                if (embedding != null && embedding.Length > 0)
                {
                    // Use raw SQL to update the embedding directly in the database
                    var embeddingString = string.Join(",", embedding);

                    // Use string interpolation to avoid parameterization issues with vector type
                    var sql = $"UPDATE quotes SET embedding = '[{embeddingString}]'::vector WHERE quote_id = {quote.QuoteId}";
                    await _context.Database.ExecuteSqlRawAsync(sql);

                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to vectorize quote {QuoteId}", quote.QuoteId);
            }
        }

        _logger.LogInformation("Completed vectorization of {Count} quotes", count);
        return count;
    }

    public async Task<List<SupplierVectorModel>> FindSimilarSuppliersAsync(string query, int limit = 10)
    {
        try
        {
            // Use raw SQL to query suppliers with embeddings
            var suppliers = await _context.Suppliers
                .FromSqlRaw(@"
                    SELECT * FROM suppliers 
                    WHERE embedding IS NOT NULL 
                    ORDER BY supplier_id 
                    LIMIT {0}", limit)
                .Include(s => s.SupplierCapabilities)
                .ToListAsync();

            return suppliers.Select(s => SupplierVectorModel.FromSupplier(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar suppliers for query: {Query}", query);
            return new List<SupplierVectorModel>();
        }
    }

    public async Task<List<ItemVectorModel>> FindSimilarItemsAsync(string query, int limit = 10)
    {
        try
        {
            var items = await _context.Items
                .FromSqlRaw(@"
                    SELECT * FROM items 
                    WHERE embedding IS NOT NULL 
                    ORDER BY item_id 
                    LIMIT {0}", limit)
                .Include(i => i.ItemSpecifications)
                .ToListAsync();

            return items.Select(i => ItemVectorModel.FromItem(i)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar items for query: {Query}", query);
            return new List<ItemVectorModel>();
        }
    }

    public async Task<List<RfqVectorModel>> FindSimilarRfqsAsync(string query, int limit = 10)
    {
        try
        {
            var rfqs = await _context.RequestForQuotes
                .FromSqlRaw(@"
                    SELECT * FROM request_for_quotes 
                    WHERE embedding IS NOT NULL 
                    ORDER BY rfq_id 
                    LIMIT {0}", limit)
                .Include(rfq => rfq.RfqLineItems)
                .ThenInclude(rli => rli.Item)
                .ToListAsync();

            return rfqs.Select(rfq => RfqVectorModel.FromRequestForQuote(rfq)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar RFQs for query: {Query}", query);
            return new List<RfqVectorModel>();
        }
    }

    public async Task<List<QuoteVectorModel>> FindSimilarQuotesAsync(string query, int limit = 10)
    {
        try
        {
            var quotes = await _context.Quotes
                .FromSqlRaw(@"
                    SELECT * FROM quotes 
                    WHERE embedding IS NOT NULL 
                    ORDER BY quote_id 
                    LIMIT {0}", limit)
                .Include(q => q.Supplier)
                .ToListAsync();

            return quotes.Select(q => QuoteVectorModel.FromQuote(q)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar quotes for query: {Query}", query);
            return new List<QuoteVectorModel>();
        }
    }

    public async Task<VectorStoreSearchResult> PerformSemanticSearchAsync(string query, int limit = 20)
    {
        var result = new VectorStoreSearchResult();

        try
        {
            // Search suppliers
            var suppliers = await _context.Suppliers
                .FromSqlRaw(@"
                    SELECT * FROM suppliers 
                    WHERE embedding IS NOT NULL 
                    ORDER BY supplier_id 
                    LIMIT {0}", limit / 4)
                .Include(s => s.SupplierCapabilities)
                .ToListAsync();
            result.Suppliers = suppliers.Select(s => SupplierVectorModel.FromSupplier(s)).ToList();

            // Search items
            var items = await _context.Items
                .FromSqlRaw(@"
                    SELECT * FROM items 
                    WHERE embedding IS NOT NULL 
                    ORDER BY item_id 
                    LIMIT {0}", limit / 4)
                .Include(i => i.ItemSpecifications)
                .ToListAsync();
            result.Items = items.Select(i => ItemVectorModel.FromItem(i)).ToList();

            // Search RFQs
            var rfqs = await _context.RequestForQuotes
                .FromSqlRaw(@"
                    SELECT * FROM request_for_quotes 
                    WHERE embedding IS NOT NULL 
                    ORDER BY rfq_id 
                    LIMIT {0}", limit / 4)
                .Include(rfq => rfq.RfqLineItems)
                .ThenInclude(rli => rli.Item)
                .ToListAsync();
            result.RequestForQuotes = rfqs.Select(rfq => RfqVectorModel.FromRequestForQuote(rfq)).ToList();

            // Search quotes
            var quotes = await _context.Quotes
                .FromSqlRaw(@"
                    SELECT * FROM quotes 
                    WHERE embedding IS NOT NULL 
                    ORDER BY quote_id 
                    LIMIT {0}", limit / 4)
                .Include(q => q.Supplier)
                .ToListAsync();
            result.Quotes = quotes.Select(q => QuoteVectorModel.FromQuote(q)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search for query: {Query}", query);
        }

        return result;
    }

    public async Task UpdateSupplierVectorAsync(Supplier supplier)
    {
        try
        {
            var vectorModel = SupplierVectorModel.FromSupplier(supplier);
            if (vectorModel.Embedding.HasValue)
            {
                supplier.Embedding = vectorModel.Embedding.Value.ToArray();
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update supplier vector for {SupplierId}", supplier.SupplierId);
        }
    }

    public async Task UpdateItemVectorAsync(Item item)
    {
        try
        {
            var vectorModel = ItemVectorModel.FromItem(item);
            if (vectorModel.Embedding.HasValue)
            {
                item.Embedding = vectorModel.Embedding.Value.ToArray();
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update item vector for {ItemId}", item.ItemId);
        }
    }

    public async Task UpdateRfqVectorAsync(RequestForQuote rfq)
    {
        try
        {
            var vectorModel = RfqVectorModel.FromRequestForQuote(rfq);
            if (vectorModel.Embedding.HasValue)
            {
                rfq.Embedding = vectorModel.Embedding.Value.ToArray();
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update RFQ vector for {RfqId}", rfq.RfqId);
        }
    }

    public async Task UpdateQuoteVectorAsync(Quote quote)
    {
        try
        {
            var vectorModel = QuoteVectorModel.FromQuote(quote);
            if (vectorModel.Embedding.HasValue)
            {
                quote.Embedding = vectorModel.Embedding.Value.ToArray();
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quote vector for {QuoteId}", quote.QuoteId);
        }
    }
}

/// <summary>
/// Interface for vector store operations
/// </summary>
public interface IVectorStoreService
{
    Task<int> VectorizeAllSuppliersAsync();
    Task<int> VectorizeAllItemsAsync();
    Task<int> VectorizeAllRfqsAsync();
    Task<int> VectorizeAllQuotesAsync();
    Task<List<SupplierVectorModel>> FindSimilarSuppliersAsync(string query, int limit = 10);
    Task<List<ItemVectorModel>> FindSimilarItemsAsync(string query, int limit = 10);
    Task<List<RfqVectorModel>> FindSimilarRfqsAsync(string query, int limit = 10);
    Task<List<QuoteVectorModel>> FindSimilarQuotesAsync(string query, int limit = 10);
    Task<VectorStoreSearchResult> PerformSemanticSearchAsync(string query, int limit = 20);
    Task UpdateSupplierVectorAsync(Supplier supplier);
    Task UpdateItemVectorAsync(Item item);
    Task UpdateRfqVectorAsync(RequestForQuote rfq);
    Task UpdateQuoteVectorAsync(Quote quote);
}