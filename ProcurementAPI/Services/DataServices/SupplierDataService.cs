using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services.DataServices;

public class SupplierDataService : ISupplierDataService
{
    private readonly ProcurementDbContext _context;
    private readonly ILogger<SupplierDataService> _logger;

    public SupplierDataService(ProcurementDbContext context, ILogger<SupplierDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedResult<SupplierDto>> GetSuppliersAsync(
        int page,
        int pageSize,
        string? search,
        string? country,
        int? minRating,
        bool? isActive)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.CompanyName.Contains(search) ||
                s.SupplierCode.Contains(search) ||
                s.ContactName!.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(s => s.Country == country);
        }

        if (minRating.HasValue)
        {
            query = query.Where(s => s.Rating >= minRating.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and projection
        var suppliers = await query
            .OrderBy(s => s.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierDto
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.SupplierCode,
                CompanyName = s.CompanyName,
                ContactName = s.ContactName,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                City = s.City,
                State = s.State,
                Country = s.Country,
                PostalCode = s.PostalCode,
                TaxId = s.TaxId,
                PaymentTerms = s.PaymentTerms,
                CreditLimit = s.CreditLimit,
                Rating = s.Rating,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResult<SupplierDto>
        {
            Data = suppliers,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.SupplierId == id)
            .Select(s => new SupplierDto
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.SupplierCode,
                CompanyName = s.CompanyName,
                ContactName = s.ContactName,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                City = s.City,
                State = s.State,
                Country = s.Country,
                PostalCode = s.PostalCode,
                TaxId = s.TaxId,
                PaymentTerms = s.PaymentTerms,
                CreditLimit = s.CreditLimit,
                Rating = s.Rating,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Supplier?> GetSupplierEntityByIdAsync(int id)
    {
        return await _context.Suppliers.FindAsync(id);
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return supplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        supplier.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();

        return supplier;
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return false;

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SupplierExistsAsync(int id)
    {
        return await _context.Suppliers.AnyAsync(s => s.SupplierId == id);
    }

    public async Task<bool> SupplierCodeExistsAsync(string supplierCode, int? excludeId = null)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.SupplierCode == supplierCode);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.SupplierId != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.Country))
            .Select(s => s.Country!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<int> GetTotalSuppliersCountAsync()
    {
        return await _context.Suppliers.CountAsync();
    }
}