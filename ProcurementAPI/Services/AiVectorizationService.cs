using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;
using ProcurementAPI.Data;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services;

/// <summary>
/// Service for handling AI vectorization operations using Ollama
/// </summary>
public class AiVectorizationService : IAiVectorizationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiVectorizationService> _logger;
    private readonly OllamaApiClient _ollamaClient;
    private readonly IConfiguration _configuration;
    private const string ModelName = "nomic-embed-text"; // Using specialized embedding model
    private const int EmbeddingDimension = 768; // nomic-embed-text uses 768 dimensions

    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(5) // Increase timeout for embedding requests
    };

    public AiVectorizationService(
        IServiceProvider serviceProvider,
        ILogger<AiVectorizationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        // Initialize Ollama client - use environment-based configuration
        var ollamaUrl = GetOllamaUrl(configuration);
        _ollamaClient = new OllamaApiClient(ollamaUrl);
    }

    private string GetOllamaUrl(IConfiguration configuration)
    {
        var url = configuration["Ollama:Url"] ??
                  (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Docker"
                      ? "http://procurement-ollama:11434"
                      : "http://localhost:11434");

        _logger.LogInformation("Using Ollama URL: {Url} (Environment: {Environment})",
            url, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

        return url;
    }

    public async Task<int> VectorizeAllSuppliersAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting vectorization of all suppliers");

        // Get initial list of suppliers that need vectorization
        using var initialScope = _serviceProvider.CreateScope();
        var initialContext = initialScope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        var suppliersToProcess = await initialContext.Suppliers
            .Include(s => s.SupplierCapabilities)
            .Where(s => s.IsActive)
            .Take(300)
            .Select(s => new { s.SupplierId, s.SupplierCode })
            .ToListAsync(ct);

        var count = 0;
        var semaphore = new SemaphoreSlim(100); // Limit to 100 concurrent tasks
        var tasks = new List<Task>();

        foreach (var supplierInfo in suppliersToProcess)
        {
            await semaphore.WaitAsync(ct);

            var task = Task.Run(async () =>
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    // Create a new scope and context for each task
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

                    // Get the full supplier entity for this task
                    var supplier = await context.Suppliers
                        .Include(s => s.SupplierCapabilities)
                        .FirstOrDefaultAsync(s => s.SupplierId == supplierInfo.SupplierId, ct);

                    if (supplier == null)
                    {
                        _logger.LogWarning("Supplier {SupplierId} not found during vectorization", supplierInfo.SupplierId);
                        return;
                    }

                    var embedding = await GenerateSupplierEmbeddingAsync(supplier);
                    if (embedding != null)
                    {
                        supplier.Embedding = embedding;
                        await context.SaveChangesAsync(ct);
                        Interlocked.Increment(ref count);
                        _logger.LogInformation("Completed vectorization of supplier {SupplierId} {SupplierCode}", supplier.SupplierId, supplier.SupplierCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate embedding for supplier {SupplierId}", supplierInfo.SupplierId);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("Completed vectorization of {Count} suppliers", count);
        return count;
    }

    public async Task<int> VectorizeAllItemsAsync()
    {
        _logger.LogInformation("Starting vectorization of all items");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        var items = await context.Items
            .Include(i => i.ItemSpecifications)
            .Where(i => i.IsActive)
            .ToListAsync();

        var count = 0;
        foreach (var item in items)
        {
            try
            {
                var embedding = await GenerateItemEmbeddingAsync(item);
                if (embedding != null)
                {
                    item.Embedding = embedding;
                    count++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embedding for item {ItemId}", item.ItemId);
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Completed vectorization of {Count} items", count);
        return count;
    }

    public async Task<float[]?> GenerateSupplierEmbeddingAsync(Supplier supplier)
    {
        var text = supplier.GetEmbeddingText();
        return await GenerateEmbeddingAsync(text);
    }

    public async Task<float[]?> GenerateItemEmbeddingAsync(Item item)
    {
        var text = item.GetEmbeddingText();
        return await GenerateEmbeddingAsync(text);
    }

    public async Task<float[]?> GenerateRfqEmbeddingAsync(RequestForQuote rfq)
    {
        var text = rfq.GetEmbeddingText();
        return await GenerateEmbeddingAsync(text);
    }

    public async Task<float[]?> GenerateQuoteEmbeddingAsync(Quote quote)
    {
        var text = quote.GetEmbeddingText();
        return await GenerateEmbeddingAsync(text);
    }

    public async Task<float[]?> GenerateQueryEmbeddingAsync(string query)
    {
        return await GenerateEmbeddingAsync(query);
    }

    public async Task<List<Supplier>> FindSimilarSuppliersAsync(string query, int limit = 10)
    {
        var queryEmbedding = await GenerateQueryEmbeddingAsync(query);
        if (queryEmbedding == null)
            return new List<Supplier>();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        // Use raw SQL for vector similarity search
        var suppliers = await context.Suppliers
            .FromSqlRaw(@"
                SELECT *, embedding <=> {0}::vector as distance 
                FROM suppliers 
                WHERE embedding IS NOT NULL 
                ORDER BY distance 
                LIMIT {1}",
                FormatVectorForPostgres(queryEmbedding), limit)
            .Include(s => s.SupplierCapabilities)
            .ToListAsync();

        return suppliers;
    }

    public async Task<List<Item>> FindSimilarItemsAsync(string query, int limit = 10)
    {
        var queryEmbedding = await GenerateQueryEmbeddingAsync(query);
        if (queryEmbedding == null)
            return new List<Item>();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        // Use raw SQL for vector similarity search
        var items = await context.Items
            .FromSqlRaw(@"
                SELECT *, embedding <=> {0}::vector as distance 
                FROM items 
                WHERE embedding IS NOT NULL 
                ORDER BY distance 
                LIMIT {1}",
                FormatVectorForPostgres(queryEmbedding), limit)
            .Include(i => i.ItemSpecifications)
            .ToListAsync();

        return items;
    }

    public async Task<SemanticSearchResult> PerformSemanticSearchAsync(string query, int limit = 20)
    {
        var queryEmbedding = await GenerateQueryEmbeddingAsync(query);
        if (queryEmbedding == null)
            return new SemanticSearchResult();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        var result = new SemanticSearchResult();

        // Search suppliers
        result.Suppliers = await context.Suppliers
            .FromSqlRaw(@"
                SELECT *, embedding <=> {0}::vector as distance 
                FROM suppliers 
                WHERE embedding IS NOT NULL 
                ORDER BY distance 
                LIMIT {1}",
                FormatVectorForPostgres(queryEmbedding), limit / 4)
            .Include(s => s.SupplierCapabilities)
            .ToListAsync();

        // Search items
        result.Items = await context.Items
            .FromSqlRaw(@"
                SELECT *, embedding <=> {0}::vector as distance 
                FROM items 
                WHERE embedding IS NOT NULL 
                ORDER BY distance 
                LIMIT {1}",
                FormatVectorForPostgres(queryEmbedding), limit / 4)
            .Include(i => i.ItemSpecifications)
            .ToListAsync();

        // Search RFQs
        result.RequestForQuotes = await context.RequestForQuotes
            .FromSqlRaw(@"
                SELECT *, embedding <=> {0}::vector as distance 
                FROM request_for_quotes 
                WHERE embedding IS NOT NULL 
                ORDER BY distance 
                LIMIT {1}",
                FormatVectorForPostgres(queryEmbedding), limit / 4)
            .ToListAsync();

        // Search quotes
        result.Quotes = await context.Quotes
            .FromSqlRaw(@"
                SELECT *, embedding <=> {0}::vector as distance 
                FROM quotes 
                WHERE embedding IS NOT NULL 
                ORDER BY distance 
                LIMIT {1}",
                FormatVectorForPostgres(queryEmbedding), limit / 4)
            .ToListAsync();

        return result;
    }

    private async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Use direct HTTP call to Ollama for embeddings
            var request = new
            {
                model = ModelName,
                prompt = text
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Use the same URL logic as the constructor
            var ollamaUrl = GetOllamaUrl(_configuration);
            var response = await httpClient.PostAsync($"{ollamaUrl}/api/embeddings", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to generate embedding. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received embedding response: {ResponseLength} characters", responseJson.Length);

            var embeddingResponse = System.Text.Json.JsonSerializer.Deserialize<EmbeddingResponse>(responseJson);

            if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
            {
                _logger.LogWarning("No embedding generated for text: {Text}. Response: {Response}", text, responseJson);
                return null;
            }

            // Ensure we have the right dimension
            var embeddingArray = embeddingResponse.Embedding.Take(EmbeddingDimension).ToArray();
            if (embeddingArray.Length < EmbeddingDimension)
            {
                // Pad with zeros if needed
                Array.Resize(ref embeddingArray, EmbeddingDimension);
            }

            return embeddingArray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text: {Text}", text);
            return null;
        }
    }

    /// <summary>
    /// Converts a float array to PostgreSQL vector format string
    /// </summary>
    /// <param name="embedding">The embedding array</param>
    /// <returns>Vector string in PostgreSQL format like "[1.0, 2.0, 3.0, ...]"</returns>
    private string FormatVectorForPostgres(float[] embedding)
    {
        if (embedding == null || embedding.Length == 0)
            return "[]";

        return "[" + string.Join(", ", embedding.Select(f => f.ToString("F6"))) + "]";
    }

    private class EmbeddingResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}