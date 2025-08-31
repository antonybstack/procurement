using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace ProcurementAPI.Models;

[Table("request_for_quotes")]
public class RequestForQuote
{
    [Key]
    [Column("rfq_id")]
    public int RfqId { get; set; }

    [Required]
    [Column("rfq_number")]
    [StringLength(50)]
    public string RfqNumber { get; set; } = string.Empty;

    [Required]
    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public RfqStatus Status { get; set; } = RfqStatus.Draft;

    [Required]
    [Column("issue_date")]
    public DateOnly IssueDate { get; set; }

    [Required]
    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("award_date")]
    public DateOnly? AwardDate { get; set; }

    [Column("total_estimated_value", TypeName = "decimal(15,2)")]
    public decimal? TotalEstimatedValue { get; set; }

    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [Column("terms_and_conditions")]
    public string? TermsAndConditions { get; set; }

    [Column("created_by")]
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<RfqLineItem> RfqLineItems { get; set; } = new List<RfqLineItem>();
    public virtual ICollection<RfqSupplier> RfqSuppliers { get; set; } = new List<RfqSupplier>();
    public virtual ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}