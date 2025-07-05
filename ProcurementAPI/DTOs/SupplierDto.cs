namespace ProcurementAPI.DTOs;

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public int? Rating { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupplierSummaryDto
{
    public int SupplierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public int TotalQuotes { get; set; }
    public int AwardedQuotes { get; set; }
    public decimal? AvgQuotePrice { get; set; }
    public int TotalPurchaseOrders { get; set; }
    public decimal? TotalPoValue { get; set; }
}