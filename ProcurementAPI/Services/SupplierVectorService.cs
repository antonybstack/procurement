using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.PgVector;
using ProcurementAPI.Models;
using ProcurementAPI.Services.DataServices;

namespace ProcurementAPI.Services;

public class SupplierVectorService : ISupplierVectorService
{
    private readonly string _collectionName = "sksuppliers10";
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<SupplierVectorService> _logger;
    private readonly ISupplierDataService _supplierDataService;
    private readonly PostgresVectorStore _vectorStore;

    public SupplierVectorService(
        ISupplierDataService supplierDataService,
        ILogger<SupplierVectorService> logger,
        PostgresVectorStore vectorStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _supplierDataService = supplierDataService;
        _logger = logger;
        _vectorStore = vectorStore;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<IList<SupplierVector>> VectorizeSuppliersAsync(int? count)
    {
        await InitializeVectorStoreAsync();

        // fetch suppliers from data service
        var suppliers = await _supplierDataService.GetSuppliersAsync(1, 1000, null, null, null, null);
        var supplierVectors = suppliers.Data.Select(s => new SupplierVector
        {
            SupplierId = s.SupplierId,
            CompanyName = s.CompanyName,
            SupplierCode = s.SupplierCode,
            ContactName = s.ContactName,
            Email = s.Email,
            Address = s.Address,
            City = s.City,
            State = s.State,
            Country = s.Country,
            PaymentTerms = s.PaymentTerms,
            Rating = s.Rating,
            IsActive = s.IsActive
        }).ToList();

        foreach (var record in supplierVectors) record.EmbeddingText = record.BuildSearchableContent();

        var collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        await collection.UpsertAsync(supplierVectors);

        return supplierVectors;
    }

    public async Task InitializeVectorStoreAsync()
    {
        var collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        await collection.EnsureCollectionExistsAsync();
    }

    public async Task<IList<SupplierVector>> CreateTestDataAsync()
    {
        var collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        await collection.EnsureCollectionExistsAsync();

        // Create test data
        string[] supplierCodes =
        {
            "SupplierCodeABC123",
            "SupplierCodeXYZ789",
            "SupplierCodeLMN456"
        };

        // Create records with test data
        var records = supplierCodes.Select((supplierCode, index) => new SupplierVector
        {
            SupplierId = index + 1,
            SupplierCode = supplierCode,
            CompanyName = $"Company for {supplierCode}",
            ContactName = $"Contact {index + 1}",
            Email = $"contact{index + 1}@{supplierCode.ToLower()}.com",
            Address = $"{index + 1} {supplierCode} St",
            City = $"CityName {index + 1}",
            State = $"StateName {index + 1}",
            Country = $"CountryName {index + 1}",
            IsActive = true,
            PaymentTerms = "Net 30",
            Rating = index % 5 + 1
        }).ToList();

        // Generate embeddings for each record
        foreach (var record in records) record.EmbeddingText = record.BuildSearchableContent();
        // record.Embedding = (await _embeddingGenerator.GenerateAsync(record.BuildSearchableContent())).Vector;
        // Upsert records to vector store
        await collection.UpsertAsync(records);

        _logger.LogInformation("Created and upserted {Count} test supplier vectors", records.Count);
        return records;
    }

    // public async Task<IAsyncEnumerable<VectorSearchResult<SupplierVector>>> SearchByHybridAsync(string searchValue,
    //     int top,
    //     CancellationToken cancellationToken)
    // {
    //     // TODO: Implement hybrid search combining vector and keyword search
    //     // Depends on implement of NpgsqlTsVector: https://www.npgsql.org/efcore/mapping/full-text-search.html
    //     var collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
    //     return collection.SearchAsync(searchValue, top);
    // }

    public async Task<IAsyncEnumerable<VectorSearchResult<SupplierVector>>> SearchByVectorAsync(
        string searchValue,
        int top,
        CancellationToken cancellationToken)
    {
        var collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        return collection.SearchAsync(searchValue, top, null, cancellationToken);
    }
}