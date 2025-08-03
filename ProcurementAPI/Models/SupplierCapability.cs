using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

[Table("supplier_capabilities")]
public class SupplierCapability
{
    [Key]
    [Column("capability_id")]
    public int CapabilityId { get; set; }

    [Required]
    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Required]
    [Column("capability_type")]
    [StringLength(100)]
    public string CapabilityType { get; set; } = string.Empty;

    [Required]
    [Column("capability_value")]
    public string CapabilityValue { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("SupplierId")]
    public virtual Supplier Supplier { get; set; } = null!;
}