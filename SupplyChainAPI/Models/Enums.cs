namespace SupplyChainAPI.Models;

public enum QuoteStatus
{
    Pending,
    Submitted,
    Awarded,
    Rejected,
    Expired
}

public enum RfqStatus
{
    Draft,
    Published,
    Closed,
    Awarded,
    Cancelled
}

public enum PoStatus
{
    Draft,
    Sent,
    Confirmed,
    Received,
    Cancelled
}

public enum ItemCategory
{
    Electronics,
    Machinery,
    RawMaterials,
    Packaging,
    Services,
    Components
} 