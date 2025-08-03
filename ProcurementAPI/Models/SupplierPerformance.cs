using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

public class SupplierPerformance
{
    [Key]
    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Column("company_name", TypeName = "text")]
    public string? CompanyName { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("total_quotes")]
    public int TotalQuotes { get; set; }

    [Column("awarded_quotes")]
    public int AwardedQuotes { get; set; }

    [Column("avg_quote_price", TypeName = "numeric")]
    public decimal? AvgQuotePrice { get; set; }

    [Column("total_purchase_orders")]
    public int TotalPurchaseOrders { get; set; }

    [Column("total_po_value", TypeName = "numeric")]
    public decimal? TotalPoValue { get; set; }
}
