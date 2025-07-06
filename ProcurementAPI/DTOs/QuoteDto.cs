namespace ProcurementAPI.DTOs;

public class QuoteDto
{
    public int QuoteId { get; set; }
    public int RfqId { get; set; }
    public int SupplierId { get; set; }
    public int LineItemId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int QuantityOffered { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? PaymentTerms { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? TechnicalComplianceNotes { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateOnly? ValidUntilDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public SupplierDto Supplier { get; set; } = new();
    public RfqLineItemDto RfqLineItem { get; set; } = new();
}

public class QuoteCreateDto
{
    public int RfqId { get; set; }
    public int SupplierId { get; set; }
    public int LineItemId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int QuantityOffered { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? PaymentTerms { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? TechnicalComplianceNotes { get; set; }
    public DateOnly? ValidUntilDate { get; set; }
}

public class QuoteUpdateDto
{
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int QuantityOffered { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? PaymentTerms { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? TechnicalComplianceNotes { get; set; }
    public DateOnly? ValidUntilDate { get; set; }
}

public class QuoteSummaryDto
{
    public int QuoteId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int QuantityOffered { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public DateTime SubmittedDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string RfqNumber { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
}