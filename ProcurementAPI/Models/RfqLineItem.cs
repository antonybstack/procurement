using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

[Table("rfq_line_items")]
public class RfqLineItem
{
    [Key]
    [Column("line_item_id")]
    public int LineItemId { get; set; }

    [Required]
    [Column("rfq_id")]
    public int RfqId { get; set; }

    [Column("item_id")]
    public int? ItemId { get; set; }

    [Required]
    [Column("line_number")]
    public int LineNumber { get; set; }

    [Required]
    [Column("quantity_required")]
    public int QuantityRequired { get; set; }

    [Required]
    [Column("unit_of_measure")]
    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("technical_specifications")]
    public string? TechnicalSpecifications { get; set; }

    [Column("delivery_date")]
    public DateOnly? DeliveryDate { get; set; }

    [Column("estimated_unit_cost", TypeName = "decimal(15,2)")]
    public decimal? EstimatedUnitCost { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // AI Vectorization
    [NotMapped]
    public float[]? Embedding { get; set; }

    // Navigation properties
    [ForeignKey("RfqId")]
    public virtual RequestForQuote RequestForQuote { get; set; } = null!;

    [ForeignKey("ItemId")]
    public virtual Item? Item { get; set; }

    public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();

    /// <summary>
    /// Generates text for embedding generation
    /// </summary>
    public string GetEmbeddingText()
    {
        var parts = new List<string>
        {
            LineNumber.ToString(),
            QuantityRequired.ToString(),
            UnitOfMeasure,
            Description ?? "",
            TechnicalSpecifications ?? "",
            DeliveryDate?.ToString() ?? "",
            EstimatedUnitCost?.ToString() ?? "",
            Item?.ItemCode ?? "",
            Item?.Description ?? "",
            Item?.Category.ToString() ?? ""
        };

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}