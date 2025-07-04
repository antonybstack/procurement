namespace SupplyChainAPI.DTOs;

public class QuoteDto
{
    public int QuoteId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int QuantityOffered { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? PaymentTerms { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateOnly? ValidUntilDate { get; set; }
    public SupplierDto Supplier { get; set; } = new();
    public RfqLineItemDto RfqLineItem { get; set; } = new();
} 