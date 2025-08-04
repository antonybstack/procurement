using Microsoft.Extensions.VectorData;

namespace ProcurementAPI.Models;

/// <summary>
/// Vector store model for Supplier embeddings using Semantic Kernel
/// </summary>
public class SupplierVectorModel
{
    [VectorStoreKey]
    public int SupplierId { get; set; }

    [VectorStoreData(StorageName = "supplier_code")]
    public string SupplierCode { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "contact_name")]
    public string? ContactName { get; set; }

    [VectorStoreData(StorageName = "email")]
    public string? Email { get; set; }

    [VectorStoreData(StorageName = "phone")]
    public string? Phone { get; set; }

    [VectorStoreData(StorageName = "address")]
    public string? Address { get; set; }

    [VectorStoreData(StorageName = "city")]
    public string? City { get; set; }

    [VectorStoreData(StorageName = "state")]
    public string? State { get; set; }

    [VectorStoreData(StorageName = "country")]
    public string? Country { get; set; }

    [VectorStoreData(StorageName = "postal_code")]
    public string? PostalCode { get; set; }

    [VectorStoreData(StorageName = "tax_id")]
    public string? TaxId { get; set; }

    [VectorStoreData(StorageName = "payment_terms")]
    public string? PaymentTerms { get; set; }

    [VectorStoreData(StorageName = "credit_limit")]
    public decimal? CreditLimit { get; set; }

    [VectorStoreData(StorageName = "rating")]
    public int? Rating { get; set; }

    [VectorStoreData(StorageName = "is_active")]
    public bool IsActive { get; set; }

    [VectorStoreData(StorageName = "capabilities")]
    public string? Capabilities { get; set; }

    [VectorStoreVector(Dimensions: 768, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    /// <summary>
    /// Convert from EF Core Supplier model to Vector Store model
    /// </summary>
    public static SupplierVectorModel FromSupplier(Supplier supplier)
    {
        return new SupplierVectorModel
        {
            SupplierId = supplier.SupplierId,
            SupplierCode = supplier.SupplierCode,
            CompanyName = supplier.CompanyName,
            ContactName = supplier.ContactName,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            City = supplier.City,
            State = supplier.State,
            Country = supplier.Country,
            PostalCode = supplier.PostalCode,
            TaxId = supplier.TaxId,
            PaymentTerms = supplier.PaymentTerms,
            CreditLimit = supplier.CreditLimit,
            Rating = supplier.Rating,
            IsActive = supplier.IsActive,
            Capabilities = supplier.SupplierCapabilities?.Any() == true
                ? string.Join(", ", supplier.SupplierCapabilities.Select(sc => $"{sc.CapabilityType}: {sc.CapabilityValue}"))
                : null,
            Embedding = supplier.Embedding != null ? new ReadOnlyMemory<float>(supplier.Embedding) : null
        };
    }
}

/// <summary>
/// Vector store model for Item embeddings using Semantic Kernel
/// </summary>
public class ItemVectorModel
{
    [VectorStoreKey]
    public int ItemId { get; set; }

    [VectorStoreData(StorageName = "item_code")]
    public string ItemCode { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "description")]
    public string Description { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "category")]
    public string Category { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "unit_of_measure")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "standard_cost")]
    public decimal? StandardCost { get; set; }

    [VectorStoreData(StorageName = "min_order_quantity")]
    public int MinOrderQuantity { get; set; }

    [VectorStoreData(StorageName = "lead_time_days")]
    public int LeadTimeDays { get; set; }

    [VectorStoreData(StorageName = "is_active")]
    public bool IsActive { get; set; }

    [VectorStoreData(StorageName = "specifications")]
    public string? Specifications { get; set; }

    [VectorStoreVector(Dimensions: 768, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    /// <summary>
    /// Convert from EF Core Item model to Vector Store model
    /// </summary>
    public static ItemVectorModel FromItem(Item item)
    {
        return new ItemVectorModel
        {
            ItemId = item.ItemId,
            ItemCode = item.ItemCode,
            Description = item.Description,
            Category = item.Category.ToString(),
            UnitOfMeasure = item.UnitOfMeasure,
            StandardCost = item.StandardCost,
            MinOrderQuantity = item.MinOrderQuantity,
            LeadTimeDays = item.LeadTimeDays,
            IsActive = item.IsActive,
            Specifications = item.ItemSpecifications?.Any() == true
                ? string.Join(", ", item.ItemSpecifications.Select(ispec => $"{ispec.SpecName}: {ispec.SpecValue}"))
                : null,
            Embedding = item.Embedding != null ? new ReadOnlyMemory<float>(item.Embedding) : null
        };
    }
}

/// <summary>
/// Vector store model for RFQ embeddings using Semantic Kernel
/// </summary>
public class RfqVectorModel
{
    [VectorStoreKey]
    public int RfqId { get; set; }

    [VectorStoreData(StorageName = "rfq_number")]
    public string RfqNumber { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "title")]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "description")]
    public string? Description { get; set; }

    [VectorStoreData(StorageName = "status")]
    public string Status { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "due_date")]
    public DateTime? DueDate { get; set; }

    [VectorStoreData(StorageName = "total_estimated_value")]
    public decimal? TotalEstimatedValue { get; set; }

    [VectorStoreData(StorageName = "currency")]
    public string Currency { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "line_items")]
    public string? LineItems { get; set; }

    [VectorStoreVector(Dimensions: 768, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    /// <summary>
    /// Convert from EF Core RequestForQuote model to Vector Store model
    /// </summary>
    public static RfqVectorModel FromRequestForQuote(RequestForQuote rfq)
    {
        return new RfqVectorModel
        {
            RfqId = rfq.RfqId,
            RfqNumber = rfq.RfqNumber,
            Title = rfq.Title,
            Description = rfq.Description,
            Status = rfq.Status.ToString(),
            DueDate = rfq.DueDate.ToDateTime(TimeOnly.MinValue),
            TotalEstimatedValue = rfq.TotalEstimatedValue,
            Currency = rfq.Currency,
            LineItems = rfq.RfqLineItems?.Any() == true
                ? string.Join(", ", rfq.RfqLineItems.Select(rli => $"{rli.Item?.Description}: {rli.QuantityRequired}"))
                : null,
            Embedding = rfq.Embedding != null ? new ReadOnlyMemory<float>(rfq.Embedding) : null
        };
    }
}

/// <summary>
/// Vector store model for Quote embeddings using Semantic Kernel
/// </summary>
public class QuoteVectorModel
{
    [VectorStoreKey]
    public int QuoteId { get; set; }

    [VectorStoreData(StorageName = "quote_number")]
    public string QuoteNumber { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "supplier_name")]
    public string? SupplierName { get; set; }

    [VectorStoreData(StorageName = "total_amount")]
    public decimal TotalAmount { get; set; }

    [VectorStoreData(StorageName = "status")]
    public string Status { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "valid_until")]
    public DateTime? ValidUntil { get; set; }

    [VectorStoreData(StorageName = "delivery_terms")]
    public string? DeliveryTerms { get; set; }

    [VectorStoreData(StorageName = "payment_terms")]
    public string? PaymentTerms { get; set; }

    [VectorStoreData(StorageName = "line_items")]
    public string? LineItems { get; set; }

    [VectorStoreVector(Dimensions: 768, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float>? Embedding { get; set; }

    /// <summary>
    /// Convert from EF Core Quote model to Vector Store model
    /// </summary>
    public static QuoteVectorModel FromQuote(Quote quote)
    {
        return new QuoteVectorModel
        {
            QuoteId = quote.QuoteId,
            QuoteNumber = quote.QuoteNumber,
            SupplierName = quote.Supplier?.CompanyName,
            TotalAmount = quote.TotalPrice,
            Status = quote.Status.ToString(),
            ValidUntil = quote.ValidUntilDate?.ToDateTime(TimeOnly.MinValue),
            DeliveryTerms = quote.DeliveryDate?.ToString(),
            PaymentTerms = quote.PaymentTerms,
            LineItems = quote.RfqLineItem?.Description != null
                ? $"{quote.RfqLineItem.Description}: {quote.QuantityOffered}"
                : null,
            Embedding = quote.Embedding != null ? new ReadOnlyMemory<float>(quote.Embedding) : null
        };
    }
}