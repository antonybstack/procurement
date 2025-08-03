namespace ProcurementAPI.DTOs;

public class SupplierCapabilityDto
{
    public int CapabilityId { get; set; }
    public int SupplierId { get; set; }
    public string CapabilityType { get; set; } = string.Empty;
    public string CapabilityValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public SupplierDto Supplier { get; set; } = new SupplierDto();
}
