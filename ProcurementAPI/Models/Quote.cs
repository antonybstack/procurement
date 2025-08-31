using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace ProcurementAPI.Models;

[Table("quotes")]
public class Quote
{
    [Key]
    [Column("quote_id")]
    public int QuoteId { get; set; }

    [Required]
    [Column("rfq_id")]
    public int RfqId { get; set; }

    [Required]
    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Required]
    [Column("line_item_id")]
    public int LineItemId { get; set; }

    [Required]
    [Column("quote_number")]
    [StringLength(50)]
    public string QuoteNumber { get; set; } = string.Empty;

    [Column("status")]
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;

    [Required]
    [Column("unit_price", TypeName = "decimal(15,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("total_price", TypeName = "decimal(15,2)")]
    public decimal TotalPrice { get; set; }

    [Required]
    [Column("quantity_offered")]
    public int QuantityOffered { get; set; }

    [Column("delivery_date")]
    public DateOnly? DeliveryDate { get; set; }

    [Column("payment_terms")]
    [StringLength(100)]
    public string? PaymentTerms { get; set; }

    [Column("warranty_period_months")]
    public int? WarrantyPeriodMonths { get; set; }

    [Column("technical_compliance_notes")]
    public string? TechnicalComplianceNotes { get; set; }

    [Column("submitted_date")]
    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

    [Column("valid_until_date")]
    public DateOnly? ValidUntilDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RfqId")]
    public virtual RequestForQuote RequestForQuote { get; set; } = null!;

    [ForeignKey("SupplierId")]
    public virtual Supplier Supplier { get; set; } = null!;

    [ForeignKey("LineItemId")]
    public virtual RfqLineItem RfqLineItem { get; set; } = null!;

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
}