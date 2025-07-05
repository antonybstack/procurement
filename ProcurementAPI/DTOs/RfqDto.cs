namespace ProcurementAPI.DTOs;

public class RfqDto
{
    public int RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? AwardDate { get; set; }
    public decimal? TotalEstimatedValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineItemsCount { get; set; }
    public int SuppliersInvited { get; set; }
    public int QuotesReceived { get; set; }
}

public class RfqLineItemDto
{
    public int LineItemId { get; set; }
    public int LineNumber { get; set; }
    public int QuantityRequired { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public decimal? EstimatedUnitCost { get; set; }
    public ItemDto? Item { get; set; }
}

public class RfqDetailDto : RfqDto
{
    public List<RfqLineItemDto> LineItems { get; set; } = new();
    public List<SupplierDto> InvitedSuppliers { get; set; } = new();
    public List<QuoteDto> Quotes { get; set; } = new();
}