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

    // Navigation properties
    public virtual ICollection<RfqSupplier> RfqSuppliers { get; set; } = new List<RfqSupplier>();
    public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}