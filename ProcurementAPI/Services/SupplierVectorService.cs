using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
using Elastic.SemanticKernel.Connectors.Elasticsearch;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;
using ProcurementAPI.Services.DataServices;

namespace ProcurementAPI.Services;

public class SupplierVectorService : ISupplierVectorService
{
    private readonly string _collectionName = "sksuppliers14";
    private readonly ElasticsearchClient _elasticClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<SupplierVectorService> _logger;

    private readonly ISupplierDataService _supplierDataService;

    // private readonly PostgresVectorStore _vectorStore;
    private readonly ElasticsearchVectorStore _vectorStore;

    public SupplierVectorService(
        ISupplierDataService supplierDataService,
        ILogger<SupplierVectorService> logger,
        ElasticsearchVectorStore vectorStore,
        ElasticsearchClient elasticClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _supplierDataService = supplierDataService;
        _logger = logger;
        _vectorStore = vectorStore;
        _embeddingGenerator = embeddingGenerator;
        _elasticClient = elasticClient;
    }

    public async Task<IList<SupplierVector>> VectorizeSuppliersAsync(int? count)
    {
        await InitializeVectorStoreAsync();

        // fetch suppliers from data service
        PaginatedResult<SupplierDto> suppliers = await _supplierDataService.GetSuppliersAsync(1, 1000, null, null, null, null);
        List<SupplierVector> supplierVectors = suppliers.Data.Select(s => new SupplierVector
        {
            SupplierId = s.SupplierId.ToString(),
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

        ElasticsearchCollection<string, SupplierVector> collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        await collection.UpsertAsync(supplierVectors);

        return supplierVectors;
    }

    public async Task InitializeVectorStoreAsync()
    {
        ElasticsearchCollection<string, SupplierVector> collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        await collection.EnsureCollectionExistsAsync();
    }

    public async Task<IList<SupplierVector>> CreateTestDataAsync()
    {
        ElasticsearchCollection<string, SupplierVector> collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        await collection.EnsureCollectionExistsAsync();

        // Create test data
        string[] supplierCodes =
        {
            "SupplierCodeABC123",
            "SupplierCodeXYZ789",
            "SupplierCodeLMN456"
        };

        // Create records with test data
        List<SupplierVector> records = supplierCodes.Select((supplierCode, index) => new SupplierVector
        {
            SupplierId = (index + 1).ToString(),
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


    public async Task<IAsyncEnumerable<Supplier>> SearchByVectorAsync(
        string searchValue,
        int top,
        CancellationToken cancellationToken)
    {
        Debug.Assert(top > 0, "Top must be greater than 0");
        Debug.Assert(searchValue.Length > 0, "Search value must be greater than 0");
        top = Math.Min(top, 1);
        top = Math.Max(top, 20);
        if (string.IsNullOrEmpty(searchValue)) return AsyncEnumerable.Empty<Supplier>();
        ElasticsearchCollection<string, SupplierVector> collection = _vectorStore.GetCollection<string, SupplierVector>(_collectionName);
        IAsyncEnumerable<VectorSearchResult<SupplierVector>> supplierVectorDtos = collection.SearchAsync(searchValue, top, null, cancellationToken);
        return supplierVectorDtos.Select(s => new Supplier
        {
            SupplierId = int.TryParse(s.Record.SupplierId, out var id) ? id : 0,
            CompanyName = s.Record.CompanyName ?? string.Empty,
            SupplierCode = s.Record.SupplierCode ?? string.Empty,
            ContactName = s.Record.ContactName,
            Email = s.Record.Email,
            Address = s.Record.Address,
            City = s.Record.City,
            State = s.Record.State,
            Country = s.Record.Country,
            PaymentTerms = s.Record.PaymentTerms,
            Rating = s.Record.Rating,
            IsActive = s.Record.IsActive
        });
    }

    public async Task<IAsyncEnumerable<Supplier>> SearchByKeywordAsync(
        string searchValue,
        int top,
        CancellationToken cancellationToken)
    {
        Debug.Assert(top > 0, "Top must be greater than 0");
        Debug.Assert(searchValue.Length > 0, "Search value must be greater than 0");
        top = Math.Min(top, 1);
        top = Math.Max(top, 20);
        if (string.IsNullOrEmpty(searchValue)) return AsyncEnumerable.Empty<Supplier>();

        try
        {
            // Use Elasticsearch's query_string query for full-text search across multiple fields
            var searchResponse = await _elasticClient.SearchAsync<SupplierVector>(s => s
                .Index(_collectionName)
                .Size(top)
                .Query(q => q
                    .QueryString(qs => qs
                        .Query($"({searchValue})")
                        .Fields(new[] { "COMPANY_NAME", "SUPPLIER_CODE", "CONTACT_NAME", "EMAIL", "EMBEDDING_TEXT" })
                        .DefaultOperator(Elastic.Clients.Elasticsearch.QueryDsl.Operator.And)
                    )
                ), cancellationToken);

            if (!searchResponse.IsValidResponse)
            {
                _logger.LogWarning("Elasticsearch search failed: {Error}", searchResponse.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");
                return AsyncEnumerable.Empty<Supplier>();
            }

            if (!searchResponse.Documents.Any())
            {
                return AsyncEnumerable.Empty<Supplier>();
            }

            var suppliers = searchResponse.Documents.Select(doc => new Supplier
            {
                SupplierId = int.TryParse(doc.SupplierId, out var id) ? id : 0,
                CompanyName = doc.CompanyName,
                SupplierCode = doc.SupplierCode,
                ContactName = doc.ContactName,
                Email = doc.Email,
                Address = doc.Address,
                City = doc.City,
                State = doc.State,
                Country = doc.Country,
                PaymentTerms = doc.PaymentTerms,
                Rating = doc.Rating,
                IsActive = doc.IsActive
            }).ToList();

            return suppliers.ToAsyncEnumerable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing keyword search for query: {SearchValue}", searchValue);
            return AsyncEnumerable.Empty<Supplier>();
        }
    }
}