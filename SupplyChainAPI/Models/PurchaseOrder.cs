using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplyChainAPI.Models;

[Table("purchase_orders")]
public class PurchaseOrder
{
    [Key]
    [Column("po_id")]
    public int PoId { get; set; }

    [Required]
    [Column("po_number")]
    [StringLength(50)]
    public string PoNumber { get; set; } = string.Empty;

    [Column("supplier_id")]
    public int? SupplierId { get; set; }

    [Column("rfq_id")]
    public int? RfqId { get; set; }

    [Column("status")]
    public PoStatus Status { get; set; } = PoStatus.Draft;

    [Required]
    [Column("order_date")]
    public DateOnly OrderDate { get; set; }

    [Column("expected_delivery_date")]
    public DateOnly? ExpectedDeliveryDate { get; set; }

    [Required]
    [Column("total_amount", TypeName = "decimal(15,2)")]
    public decimal TotalAmount { get; set; }

    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [Column("payment_terms")]
    [StringLength(100)]
    public string? PaymentTerms { get; set; }

    [Column("shipping_address")]
    public string? ShippingAddress { get; set; }

    [Column("billing_address")]
    public string? BillingAddress { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by")]
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey("RfqId")]
    public virtual RequestForQuote? RequestForQuote { get; set; }

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
}