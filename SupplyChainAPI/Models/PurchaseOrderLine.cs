using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplyChainAPI.Models;

[Table("purchase_order_lines")]
public class PurchaseOrderLine
{
    [Key]
    [Column("po_line_id")]
    public int PoLineId { get; set; }

    [Required]
    [Column("po_id")]
    public int PoId { get; set; }

    [Column("quote_id")]
    public int? QuoteId { get; set; }

    [Required]
    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("item_id")]
    public int? ItemId { get; set; }

    [Required]
    [Column("quantity_ordered")]
    public int QuantityOrdered { get; set; }

    [Required]
    [Column("unit_price", TypeName = "decimal(15,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("total_price", TypeName = "decimal(15,2)")]
    public decimal TotalPrice { get; set; }

    [Column("delivery_date")]
    public DateOnly? DeliveryDate { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PoId")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    [ForeignKey("QuoteId")]
    public virtual Quote? Quote { get; set; }

    [ForeignKey("ItemId")]
    public virtual Item? Item { get; set; }
}