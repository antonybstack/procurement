namespace ProcurementAPI.DTOs;

public class ItemDto
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? StandardCost { get; set; }
    public int MinOrderQuantity { get; set; }
    public int LeadTimeDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ItemCreateDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? StandardCost { get; set; }
    public int MinOrderQuantity { get; set; } = 1;
    public int LeadTimeDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class ItemUpdateDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal? StandardCost { get; set; }
    public int MinOrderQuantity { get; set; }
    public int LeadTimeDays { get; set; }
    public bool IsActive { get; set; }
}