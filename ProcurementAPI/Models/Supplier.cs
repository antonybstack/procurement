using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

[Table("suppliers")]
public class Supplier
{
    [Key]
    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Required]
    [Column("supplier_code")]
    [StringLength(20)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    [Column("company_name")]
    [StringLength(255)]
    public string CompanyName { get; set; } = string.Empty;

    [Column("contact_name")]
    [StringLength(255)]
    public string? ContactName { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string? Email { get; set; }

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("city")]
    [StringLength(100)]
    public string? City { get; set; }

    [Column("state")]
    [StringLength(100)]
    public string? State { get; set; }

    [Column("country")]
    [StringLength(100)]
    public string? Country { get; set; }

    [Column("postal_code")]
    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Column("tax_id")]
    [StringLength(50)]
    public string? TaxId { get; set; }

    [Column("payment_terms")]
    [StringLength(100)]
    public string? PaymentTerms { get; set; }

    [Column("credit_limit", TypeName = "decimal(15,2)")]
    public decimal? CreditLimit { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Vector embedding for semantic search
    /// </summary>
    [NotMapped]
    public float[]? Embedding { get; set; }

    // [Column("distance")]
    // public double? Distance { get; set; }

    // [Column("chunk")]
    // public string? Chunk { get; set; }

    /// <summary>
    /// Label arrays for enhanced filtering with vector search
    /// </summary>
    [Column("category_labels")]
    public string[]? CategoryLabels { get; set; }

    [Column("certification_labels")]
    public string[]? CertificationLabels { get; set; }

    [Column("process_labels")]
    public string[]? ProcessLabels { get; set; }

    [Column("material_labels")]
    public string[]? MaterialLabels { get; set; }

    [Column("service_labels")]
    public string[]? ServiceLabels { get; set; }

    // Navigation properties
    public virtual ICollection<RfqSupplier> RfqSuppliers { get; set; } = new List<RfqSupplier>();
    public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<SupplierCapability> SupplierCapabilities { get; set; } = new List<SupplierCapability>();

    /// <summary>
    /// Gets the text content for embedding generation
    /// </summary>
    public string GetEmbeddingText()
    {
        var parts = new List<string>
        {
            CompanyName,
            ContactName ?? "",
            Email ?? "",
            Phone ?? "",
            Address ?? "",
            City ?? "",
            State ?? "",
            Country ?? "",
            PaymentTerms ?? "",
            TaxId ?? ""
        };

        // Add capabilities
        if (SupplierCapabilities?.Any() == true)
        {
            parts.AddRange(SupplierCapabilities.Select(sc => $"{sc.CapabilityType}: {sc.CapabilityValue}"));
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}