using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProcurementAPI.Models;

namespace ProcurementAPI.Data;

public class ProcurementDbContext : DbContext
{
    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options)
    {
    }

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<RequestForQuote> RequestForQuotes { get; set; }
    public DbSet<RfqLineItem> RfqLineItems { get; set; }
    public DbSet<RfqSupplier> RfqSuppliers { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<SupplierCapability> SupplierCapabilities { get; set; }
    public DbSet<ItemSpecification> ItemSpecifications { get; set; }
    public DbSet<SupplierPerformance> SupplierPerformance { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SupplierPerformance>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("supplier_performance");
        });

        // Configure enum mappings
        modelBuilder.Entity<Item>()
            .Property(e => e.Category)
            .HasConversion<string>();

        modelBuilder.Entity<RequestForQuote>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Quote>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PurchaseOrder>()
            .Property(e => e.Status)
            .HasConversion<string>();

        // Configure composite keys
        modelBuilder.Entity<RfqSupplier>()
            .HasKey(rs => new { rs.RfqId, rs.SupplierId });

        // Configure unique constraints
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.SupplierCode)
            .IsUnique();

        modelBuilder.Entity<Item>()
            .HasIndex(i => i.ItemCode)
            .IsUnique();

        modelBuilder.Entity<RequestForQuote>()
            .HasIndex(rfq => rfq.RfqNumber)
            .IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.PoNumber)
            .IsUnique();

        // Configure relationships
        modelBuilder.Entity<RfqLineItem>()
            .HasOne(rli => rli.RequestForQuote)
            .WithMany(rfq => rfq.RfqLineItems)
            .HasForeignKey(rli => rli.RfqId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RfqLineItem>()
            .HasOne(rli => rli.Item)
            .WithMany(i => i.RfqLineItems)
            .HasForeignKey(rli => rli.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RfqSupplier>()
            .HasOne(rs => rs.RequestForQuote)
            .WithMany(rfq => rfq.RfqSuppliers)
            .HasForeignKey(rs => rs.RfqId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RfqSupplier>()
            .HasOne(rs => rs.Supplier)
            .WithMany(s => s.RfqSuppliers)
            .HasForeignKey(rs => rs.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quote>()
            .HasOne(q => q.RequestForQuote)
            .WithMany(rfq => rfq.Quotes)
            .HasForeignKey(q => q.RfqId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quote>()
            .HasOne(q => q.Supplier)
            .WithMany(s => s.Quotes)
            .HasForeignKey(q => q.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quote>()
            .HasOne(q => q.RfqLineItem)
            .WithMany(rli => rli.Quotes)
            .HasForeignKey(q => q.LineItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.RequestForQuote)
            .WithMany(rfq => rfq.PurchaseOrders)
            .HasForeignKey(po => po.RfqId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(pol => pol.PurchaseOrder)
            .WithMany(po => po.PurchaseOrderLines)
            .HasForeignKey(pol => pol.PoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(pol => pol.Quote)
            .WithMany(q => q.PurchaseOrderLines)
            .HasForeignKey(pol => pol.QuoteId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(pol => pol.Item)
            .WithMany(i => i.PurchaseOrderLines)
            .HasForeignKey(pol => pol.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure triggers for updated_at columns
        modelBuilder.Entity<Supplier>()
            .Property(s => s.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Item>()
            .Property(i => i.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<RequestForQuote>()
            .Property(rfq => rfq.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<RfqLineItem>()
            .Property(rli => rli.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Quote>()
            .Property(q => q.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<PurchaseOrder>()
            .Property(po => po.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<PurchaseOrderLine>()
            .Property(pol => pol.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure relationships for AI-related entities
        modelBuilder.Entity<SupplierCapability>()
            .HasOne(sc => sc.Supplier)
            .WithMany(s => s.SupplierCapabilities)
            .HasForeignKey(sc => sc.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemSpecification>()
            .HasOne(ispec => ispec.Item)
            .WithMany(i => i.ItemSpecifications)
            .HasForeignKey(ispec => ispec.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure unique constraints for AI-related entities
        modelBuilder.Entity<SupplierCapability>()
            .HasIndex(sc => new { sc.SupplierId, sc.CapabilityType, sc.CapabilityValue })
            .IsUnique();

        modelBuilder.Entity<ItemSpecification>()
            .HasIndex(ispec => new { ispec.ItemId, ispec.SpecName, ispec.SpecValue })
            .IsUnique();

        // Vector embedding properties are excluded from EF Core mapping using [NotMapped]
        // These will be handled by raw SQL queries for vector operations
    }
}