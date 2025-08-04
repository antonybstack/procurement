using System.Collections.Concurrent;
using ProcurementAPI.Data;
using ProcurementAPI.Models;

namespace ProcurementAPI.Tests;

public static class Utilities
{
    // Thread-safe ID generators for test data
    private static readonly ConcurrentDictionary<string, int> _idCounters = new();

    // Seeded data ranges (to avoid conflicts)
    private const int MAX_SEEDED_SUPPLIER_ID = 5;
    private const int MAX_SEEDED_RFQ_ID = 3;
    private const int MAX_SEEDED_LINE_ITEM_ID = 4;
    private const int MAX_SEEDED_QUOTE_ID = 5;

    /// <summary>
    /// Get a unique RFQ ID for test data (cycles through seeded RFQ IDs 1-3)
    /// </summary>
    public static int GetNextRfqId()
    {
        var counter = _idCounters.AddOrUpdate("rfq", 1, (key, oldValue) => oldValue + 1);
        return ((counter - 1) % MAX_SEEDED_RFQ_ID) + 1; // Returns 1, 2, 3, 1, 2, 3...
    }

    /// <summary>
    /// Get a unique Supplier ID for test data (cycles through seeded supplier IDs 1-5)
    /// </summary>
    public static int GetNextSupplierId()
    {
        var counter = _idCounters.AddOrUpdate("supplier", 1, (key, oldValue) => oldValue + 1);
        return ((counter - 1) % MAX_SEEDED_SUPPLIER_ID) + 1; // Returns 1, 2, 3, 4, 5, 1, 2, 3, 4, 5...
    }

    /// <summary>
    /// Get a unique Line Item ID for test data (cycles through valid seeded IDs 1-4)
    /// </summary>
    public static int GetNextLineItemId()
    {
        var counter = _idCounters.AddOrUpdate("lineItem", 1, (key, oldValue) => oldValue + 1);
        return ((counter - 1) % MAX_SEEDED_LINE_ITEM_ID) + 1; // Returns 1, 2, 3, 4, 1, 2, 3, 4...
    }

    /// <summary>
    /// Get a unique combination of (RfqId, SupplierId, LineItemId) for quote creation
    /// Uses existing seeded RFQ IDs and cycles through existing supplier IDs
    /// </summary>
    public static (int RfqId, int SupplierId, int LineItemId) GetNextQuoteCombination()
    {
        var counter = _idCounters.AddOrUpdate("quoteCombination", 1, (key, oldValue) => oldValue + 1);

        // Use existing seeded RFQ IDs (1-3)
        var rfqId = ((counter - 1) % MAX_SEEDED_RFQ_ID) + 1;
        // Cycle through existing supplier IDs (1-5)
        var supplierId = ((counter - 1) % MAX_SEEDED_SUPPLIER_ID) + 1;
        // Cycle through valid line item IDs (1-4)
        var lineItemId = ((counter - 1) % MAX_SEEDED_LINE_ITEM_ID) + 1;

        return (rfqId, supplierId, lineItemId);
    }

    /// <summary>
    /// Reset all ID counters (useful for test cleanup)
    /// </summary>
    public static void ResetIdCounters()
    {
        _idCounters.Clear();
    }

    public static void InitializeDbForTests(ProcurementDbContext db)
    {
        db.Suppliers.AddRange(GetSeedingSuppliers());
        db.Items.AddRange(GetSeedingItems());
        db.RequestForQuotes.AddRange(GetSeedingRfqs());
        db.RfqLineItems.AddRange(GetSeedingRfqLineItems());
        db.RfqSuppliers.AddRange(GetSeedingRfqSuppliers());
        db.Quotes.AddRange(GetSeedingQuotes());
        db.SaveChanges();
    }

    public static void ReinitializeDbForTests(ProcurementDbContext db)
    {
        db.Quotes.RemoveRange(db.Quotes);
        db.RfqSuppliers.RemoveRange(db.RfqSuppliers);
        db.RfqLineItems.RemoveRange(db.RfqLineItems);
        db.Suppliers.RemoveRange(db.Suppliers);
        db.Items.RemoveRange(db.Items);
        db.RequestForQuotes.RemoveRange(db.RequestForQuotes);
        db.SupplierPerformance.RemoveRange(db.SupplierPerformance);
        InitializeDbForTests(db);
    }

    public static List<Supplier> GetSeedingSuppliers()
    {
        return new List<Supplier>()
        {
            new Supplier
            {
                SupplierId = 1,
                SupplierCode = "SUP001",
                CompanyName = "Tech Solutions Inc.",
                ContactName = "John Smith",
                Email = "john.smith@techsolutions.com",
                Phone = "+1-555-0101",
                Address = "123 Tech Street",
                City = "San Francisco",
                State = "CA",
                Country = "USA",
                PostalCode = "94105",
                TaxId = "TAX123456",
                PaymentTerms = "Net 30",
                CreditLimit = 50000.00m,
                Rating = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Supplier
            {
                SupplierId = 2,
                SupplierCode = "SUP002",
                CompanyName = "Global Manufacturing Ltd.",
                ContactName = "Sarah Johnson",
                Email = "sarah.johnson@globalmfg.com",
                Phone = "+1-555-0102",
                Address = "456 Industrial Blvd",
                City = "Chicago",
                State = "IL",
                Country = "USA",
                PostalCode = "60601",
                TaxId = "TAX789012",
                PaymentTerms = "Net 45",
                CreditLimit = 75000.00m,
                Rating = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Supplier
            {
                SupplierId = 3,
                SupplierCode = "SUP003",
                CompanyName = "Quality Parts Co.",
                ContactName = "Mike Wilson",
                Email = "mike.wilson@qualityparts.com",
                Phone = "+1-555-0103",
                Address = "789 Quality Drive",
                City = "Detroit",
                State = "MI",
                Country = "USA",
                PostalCode = "48201",
                TaxId = "TAX345678",
                PaymentTerms = "Net 30",
                CreditLimit = 25000.00m,
                Rating = 3,
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Supplier
            {
                SupplierId = 4,
                SupplierCode = "SUP004",
                CompanyName = "European Electronics GmbH",
                ContactName = "Hans Mueller",
                Email = "hans.mueller@europeanelec.de",
                Phone = "+49-30-123456",
                Address = "10 Berliner Strasse",
                City = "Berlin",
                State = "Berlin",
                Country = "Germany",
                PostalCode = "10115",
                TaxId = "DE123456789",
                PaymentTerms = "Net 60",
                CreditLimit = 100000.00m,
                Rating = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Supplier
            {
                SupplierId = 5,
                SupplierCode = "SUP005",
                CompanyName = "Asian Components Ltd.",
                ContactName = "Li Wei",
                Email = "li.wei@asiancomponents.cn",
                Phone = "+86-10-87654321",
                Address = "88 Beijing Road",
                City = "Beijing",
                State = "Beijing",
                Country = "China",
                PostalCode = "100000",
                TaxId = "CN987654321",
                PaymentTerms = "Net 90",
                CreditLimit = 150000.00m,
                Rating = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };
    }

    public static List<Item> GetSeedingItems()
    {
        return new List<Item>()
        {
            new Item
            {
                ItemId = 1,
                ItemCode = "ITEM001",
                Description = "High-performance laptop",
                Category = ItemCategory.Electronics,
                UnitOfMeasure = "Each",
                StandardCost = 1200.00m,
                MinOrderQuantity = 1,
                LeadTimeDays = 7,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Item
            {
                ItemId = 2,
                ItemCode = "ITEM002",
                Description = "Office chair ergonomic",
                Category = ItemCategory.Services,
                UnitOfMeasure = "Each",
                StandardCost = 350.00m,
                MinOrderQuantity = 1,
                LeadTimeDays = 14,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Item
            {
                ItemId = 3,
                ItemCode = "ITEM003",
                Description = "Network switch 24-port",
                Category = ItemCategory.Electronics,
                UnitOfMeasure = "Each",
                StandardCost = 800.00m,
                MinOrderQuantity = 1,
                LeadTimeDays = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            }
        };
    }

    public static List<RequestForQuote> GetSeedingRfqs()
    {
        return new List<RequestForQuote>()
        {
            new RequestForQuote
            {
                RfqId = 1,
                RfqNumber = "RFQ-2024-001",
                Title = "Electronics Procurement",
                Description = "Procurement of electronic equipment for office upgrade",
                Status = RfqStatus.Draft,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                TotalEstimatedValue = 50000.00m,
                Currency = "USD",
                CreatedBy = "admin@company.com",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RequestForQuote
            {
                RfqId = 2,
                RfqNumber = "RFQ-2024-002",
                Title = "Furniture Supply",
                Description = "Office furniture and seating solutions",
                Status = RfqStatus.Published,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)),
                TotalEstimatedValue = 25000.00m,
                Currency = "USD",
                CreatedBy = "admin@company.com",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new RequestForQuote
            {
                RfqId = 3,
                RfqNumber = "RFQ-2024-003",
                Title = "Network Infrastructure",
                Description = "Network equipment and cabling",
                Status = RfqStatus.Closed,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
                TotalEstimatedValue = 75000.00m,
                Currency = "USD",
                CreatedBy = "admin@company.com",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };
    }

    public static List<RfqLineItem> GetSeedingRfqLineItems()
    {
        return new List<RfqLineItem>()
        {
            new RfqLineItem
            {
                LineItemId = 1,
                RfqId = 1,
                ItemId = 1,
                LineNumber = 1,
                QuantityRequired = 50,
                UnitOfMeasure = "Each",
                Description = "High-performance laptops for office upgrade",
                TechnicalSpecifications = "Intel i7 or better, 16GB RAM minimum, 512GB SSD",
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                EstimatedUnitCost = 1200.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RfqLineItem
            {
                LineItemId = 2,
                RfqId = 1,
                ItemId = 3,
                LineNumber = 2,
                QuantityRequired = 10,
                UnitOfMeasure = "Each",
                Description = "Network switches for office infrastructure",
                TechnicalSpecifications = "24-port Gigabit Ethernet, PoE support",
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)),
                EstimatedUnitCost = 800.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new RfqLineItem
            {
                LineItemId = 3,
                RfqId = 2,
                ItemId = 2,
                LineNumber = 1,
                QuantityRequired = 100,
                UnitOfMeasure = "Each",
                Description = "Ergonomic office chairs",
                TechnicalSpecifications = "Adjustable height, lumbar support, breathable fabric",
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)),
                EstimatedUnitCost = 350.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new RfqLineItem
            {
                LineItemId = 4,
                RfqId = 3,
                ItemId = 1,
                LineNumber = 1,
                QuantityRequired = 25,
                UnitOfMeasure = "Each",
                Description = "Network infrastructure laptops",
                TechnicalSpecifications = "Intel i5 or better, 8GB RAM, 256GB SSD",
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
                EstimatedUnitCost = 900.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };
    }

    public static List<RfqSupplier> GetSeedingRfqSuppliers()
    {
        return new List<RfqSupplier>()
        {
            new RfqSupplier
            {
                RfqId = 1,
                SupplierId = 1,
                InvitedDate = DateTime.UtcNow.AddDays(-25),
                ResponseDate = DateTime.UtcNow.AddDays(-20),
                IsResponded = true
            },
            new RfqSupplier
            {
                RfqId = 1,
                SupplierId = 2,
                InvitedDate = DateTime.UtcNow.AddDays(-25),
                ResponseDate = DateTime.UtcNow.AddDays(-18),
                IsResponded = true
            },
            new RfqSupplier
            {
                RfqId = 1,
                SupplierId = 4,
                InvitedDate = DateTime.UtcNow.AddDays(-25),
                ResponseDate = null,
                IsResponded = false
            },
            new RfqSupplier
            {
                RfqId = 2,
                SupplierId = 1,
                InvitedDate = DateTime.UtcNow.AddDays(-15),
                ResponseDate = DateTime.UtcNow.AddDays(-10),
                IsResponded = true
            },
            new RfqSupplier
            {
                RfqId = 2,
                SupplierId = 5,
                InvitedDate = DateTime.UtcNow.AddDays(-15),
                ResponseDate = null,
                IsResponded = false
            },
            new RfqSupplier
            {
                RfqId = 3,
                SupplierId = 2,
                InvitedDate = DateTime.UtcNow.AddDays(-5),
                ResponseDate = DateTime.UtcNow.AddDays(-2),
                IsResponded = true
            }
        };
    }

    public static List<Quote> GetSeedingQuotes()
    {
        return new List<Quote>()
        {
            new Quote
            {
                QuoteId = 1,
                RfqId = 1,
                SupplierId = 1,
                LineItemId = 1,
                QuoteNumber = "Q-2024-001",
                Status = QuoteStatus.Submitted,
                UnitPrice = 1150.00m,
                TotalPrice = 57500.00m,
                QuantityOffered = 50,
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                PaymentTerms = "Net 30",
                WarrantyPeriodMonths = 12,
                TechnicalComplianceNotes = "Meets all technical specifications",
                SubmittedDate = DateTime.UtcNow.AddDays(-20),
                ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Quote
            {
                QuoteId = 2,
                RfqId = 1,
                SupplierId = 2,
                LineItemId = 1,
                QuoteNumber = "Q-2024-002",
                Status = QuoteStatus.Submitted,
                UnitPrice = 1200.00m,
                TotalPrice = 60000.00m,
                QuantityOffered = 50,
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(35)),
                PaymentTerms = "Net 45",
                WarrantyPeriodMonths = 24,
                TechnicalComplianceNotes = "Exceeds specifications with extended warranty",
                SubmittedDate = DateTime.UtcNow.AddDays(-18),
                ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(75)),
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            },
            new Quote
            {
                QuoteId = 3,
                RfqId = 1,
                SupplierId = 1,
                LineItemId = 2,
                QuoteNumber = "Q-2024-003",
                Status = QuoteStatus.Awarded,
                UnitPrice = 750.00m,
                TotalPrice = 7500.00m,
                QuantityOffered = 10,
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)),
                PaymentTerms = "Net 30",
                WarrantyPeriodMonths = 12,
                TechnicalComplianceNotes = "Standard configuration",
                SubmittedDate = DateTime.UtcNow.AddDays(-15),
                ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Quote
            {
                QuoteId = 4,
                RfqId = 2,
                SupplierId = 1,
                LineItemId = 3,
                QuoteNumber = "Q-2024-004",
                Status = QuoteStatus.Submitted,
                UnitPrice = 320.00m,
                TotalPrice = 32000.00m,
                QuantityOffered = 100,
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)),
                PaymentTerms = "Net 30",
                WarrantyPeriodMonths = 36,
                TechnicalComplianceNotes = "Premium ergonomic design with extended warranty",
                SubmittedDate = DateTime.UtcNow.AddDays(-10),
                ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(80)),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Quote
            {
                QuoteId = 5,
                RfqId = 3,
                SupplierId = 2,
                LineItemId = 4,
                QuoteNumber = "Q-2024-005",
                Status = QuoteStatus.Pending,
                UnitPrice = 850.00m,
                TotalPrice = 21250.00m,
                QuantityOffered = 25,
                DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
                PaymentTerms = "Net 45",
                WarrantyPeriodMonths = 12,
                TechnicalComplianceNotes = "Standard configuration with basic warranty",
                SubmittedDate = DateTime.UtcNow.AddDays(-2),
                ValidUntilDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };
    }

    public static List<SupplierPerformance> GetSeedingSupplierPerformances()
    {
        return new List<SupplierPerformance>()
        {
            new SupplierPerformance
            {
                SupplierId = 1,
                TotalQuotes = 3,
                AwardedQuotes = 1,
                AvgQuotePrice = 1200.00m,
                TotalPurchaseOrders = 1,
                TotalPoValue = 1200.00m
            },
            new SupplierPerformance
            {
                SupplierId = 2,
                TotalQuotes = 2,
                AwardedQuotes = 1,
                AvgQuotePrice = 1200.00m,
                TotalPurchaseOrders = 1,
                TotalPoValue = 1200.00m
            },
            new SupplierPerformance
            {
                SupplierId = 3,
                TotalQuotes = 1,
                AwardedQuotes = 0,
                AvgQuotePrice = 1200.00m,
                TotalPurchaseOrders = 0,
                TotalPoValue = 0.00m
            },
            new SupplierPerformance
            {
                SupplierId = 4,
                TotalQuotes = 1,
                AwardedQuotes = 1,
                AvgQuotePrice = 1200.00m,
                TotalPurchaseOrders = 1,
                TotalPoValue = 1200.00m
            },
            new SupplierPerformance
            {
                SupplierId = 5,
                TotalQuotes = 1,
                AwardedQuotes = 0,
                AvgQuotePrice = 1200.00m,
                TotalPurchaseOrders = 0,
                TotalPoValue = 0.00m
            }
        };
    }
}