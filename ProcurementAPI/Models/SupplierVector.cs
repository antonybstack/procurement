using System.Runtime.CompilerServices;
using Microsoft.Extensions.VectorData;

namespace ProcurementAPI.Models;

/// <summary>
/// Vector store model for Supplier data with automatic embedding generation.
/// This model leverages Microsoft Semantic Kernel's PostgreSQL connector for
/// automatic embedding generation during upsert and search operations.
/// </summary>
public class SupplierVector
{
    private const int VectorDimensions = 1536;

    /// <summary>
    /// Primary key - maps to SupplierId from the main Supplier table
    /// </summary>
    [VectorStoreKey]
    public int SupplierId { get; set; }
    //
    /// <summary>
    /// Company name for searchability
    /// </summary>
    [VectorStoreData(IsFullTextIndexed = true)]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Supplier code for identification
    /// </summary>
    [VectorStoreData(IsFullTextIndexed = true)]
    public string SupplierCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Contact information
    /// </summary>
    [VectorStoreData(IsFullTextIndexed = true)]
    public string? ContactName { get; set; }
    
    /// <summary>
    /// Email address
    /// </summary>
    [VectorStoreData(IsFullTextIndexed = true)]
    public string? Email { get; set; }
    
    /// <summary>
    /// Complete address information
    /// </summary>
    [VectorStoreData]
    public string? Address { get; set; }
    
    /// <summary>
    /// City location
    /// </summary>
    [VectorStoreData]
    public string? City { get; set; }
    
    /// <summary>
    /// State/Province
    /// </summary>
    [VectorStoreData]
    public string? State { get; set; }
    
    /// <summary>
    /// Country information
    /// </summary>
    [VectorStoreData]
    public string? Country { get; set; }
    
    /// <summary>
    /// Payment terms
    /// </summary>
    [VectorStoreData]
    public string? PaymentTerms { get; set; }
    
    /// <summary>
    /// Supplier rating (1-5)
    /// </summary>
    [VectorStoreData]
    public int? Rating { get; set; }
    
    /// <summary>
    /// Active status
    /// </summary>
    [VectorStoreData]
    public bool IsActive { get; set; } = true;
    
    [VectorStoreData(IsFullTextIndexed = true)]
    public string? EmbeddingText { get; set; }
    
    // Note that the vector property is typed as a string, and
    // its value is derived from the Text property. The string
    // value will however be converted to a vector on upsert and
    // stored in the database as a vector.
    [VectorStoreVector(1536)]
    public string EmbeddingTextAuto => this.BuildSearchableContent();
    
    // [VectorStoreVector(Dimensions: VectorDimensions, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    // public ReadOnlyMemory<float> Embedding { get; set; }

    // [VectorStoreData(IsIndexed = true)]
    // public string[] Tags { get; set; }

    /// <summary>
    /// Creates searchable content from supplier properties
    /// </summary>
    public string BuildSearchableContent()
    {
        var parts = new List<string>();
    
        if (!string.IsNullOrWhiteSpace(CompanyName))
            parts.Add($"Company: {CompanyName}");
    
        if (!string.IsNullOrWhiteSpace(SupplierCode))
            parts.Add($"Code: {SupplierCode}");
    
        if (!string.IsNullOrWhiteSpace(ContactName))
            parts.Add($"Contact: {ContactName}");
    
        if (!string.IsNullOrWhiteSpace(Address))
            parts.Add($"Address: {Address}");
    
        if (!string.IsNullOrWhiteSpace(City))
            parts.Add($"City: {City}");
    
        if (!string.IsNullOrWhiteSpace(State))
            parts.Add($"State: {State}");
    
        if (!string.IsNullOrWhiteSpace(Country))
            parts.Add($"Country: {Country}");
    
        if (!string.IsNullOrWhiteSpace(PaymentTerms))
            parts.Add($"Payment Terms: {PaymentTerms}");
    
        if (Rating.HasValue)
            parts.Add($"Rating: {Rating}/5");
    
        parts.Add($"Status: {(IsActive ? "Active" : "Inactive")}");
    
        return string.Join(" | ", parts);
    }
}