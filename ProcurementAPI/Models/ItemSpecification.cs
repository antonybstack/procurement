using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

[Table("item_specifications")]
public class ItemSpecification
{
    [Key]
    [Column("spec_id")]
    public int SpecId { get; set; }

    [Required]
    [Column("item_id")]
    public int ItemId { get; set; }

    [Required]
    [Column("spec_name")]
    [StringLength(100)]
    public string SpecName { get; set; } = string.Empty;

    [Required]
    [Column("spec_value")]
    public string SpecValue { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("ItemId")]
    public virtual Item Item { get; set; } = null!;
}