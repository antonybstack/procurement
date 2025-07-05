using ProcurementAPI.Data;
using ProcurementAPI.Models;

namespace ProcurementAPI.Tests;

public static class Utilities
{
    public static void InitializeDbForTests(ProcurementDbContext db)
    {
        db.Suppliers.AddRange(GetSeedingSuppliers());
        db.Items.AddRange(GetSeedingItems());
        db.RequestForQuotes.AddRange(GetSeedingRfqs());
        db.SaveChanges();
    }

    public static void ReinitializeDbForTests(ProcurementDbContext db)
    {
        db.Suppliers.RemoveRange(db.Suppliers);
        db.Items.RemoveRange(db.Items);
        db.RequestForQuotes.RemoveRange(db.RequestForQuotes);
        InitializeDbForTests(db);
    }

    public static List<Supplier> GetSeedingSuppliers()
    {
        return new List<Supplier>()
        {
            new Supplier
            {
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
}