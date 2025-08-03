using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

[Table("items")]
public class Item
{
    [Key]
    [Column("item_id")]
    public int ItemId { get; set; }

    [Required]
    [Column("item_code")]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("category")]
    public ItemCategory Category { get; set; }

    [Required]
    [Column("unit_of_measure")]
    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Column("standard_cost", TypeName = "decimal(15,2)")]
    public decimal? StandardCost { get; set; }

    [Column("min_order_quantity")]
    public int MinOrderQuantity { get; set; } = 1;

    [Column("lead_time_days")]
    public int LeadTimeDays { get; set; } = 30;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<RfqLineItem> RfqLineItems { get; set; } = new List<RfqLineItem>();
    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
    public virtual ICollection<ItemSpecification> ItemSpecifications { get; set; } = new List<ItemSpecification>();
}