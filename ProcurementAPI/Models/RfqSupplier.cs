using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementAPI.Models;

[Table("rfq_suppliers")]
public class RfqSupplier
{
    [Key]
    [Column("rfq_id")]
    public int RfqId { get; set; }

    [Key]
    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Column("invited_date")]
    public DateTime InvitedDate { get; set; } = DateTime.UtcNow;

    [Column("response_date")]
    public DateTime? ResponseDate { get; set; }

    [Column("is_responded")]
    public bool IsResponded { get; set; } = false;

    // Navigation properties
    [ForeignKey("RfqId")]
    public virtual RequestForQuote RequestForQuote { get; set; } = null!;

    [ForeignKey("SupplierId")]
    public virtual Supplier Supplier { get; set; } = null!;
}