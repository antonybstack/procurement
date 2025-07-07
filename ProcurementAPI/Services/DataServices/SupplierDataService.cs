using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Extensions;
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
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "get_suppliers_started", "supplier",
                correlationId: correlationId,
                additionalData: new { page, pageSize, search, country, minRating, isActive });

            var query = _context.Suppliers.AsNoTracking().AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.CompanyName.Contains(search) ||
                    s.SupplierCode.Contains(search) ||
                    s.ContactName!.Contains(search));

                _logger.LogDatabaseOperation(LogLevel.Debug, "applied_search_filter", "supplier",
                    correlationId: correlationId, additionalData: new { search });
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(s => s.Country == country);
                _logger.LogDatabaseOperation(LogLevel.Debug, "applied_country_filter", "supplier",
                    correlationId: correlationId, additionalData: new { country });
            }

            if (minRating.HasValue)
            {
                query = query.Where(s => s.Rating >= minRating.Value);
                _logger.LogDatabaseOperation(LogLevel.Debug, "applied_rating_filter", "supplier",
                    correlationId: correlationId, additionalData: new { minRating });
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
                _logger.LogDatabaseOperation(LogLevel.Debug, "applied_active_filter", "supplier",
                    correlationId: correlationId, additionalData: new { isActive });
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            _logger.LogDatabaseOperation(LogLevel.Debug, "count_query_executed", "supplier",
                correlationId: correlationId, additionalData: new { totalCount });

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

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_get_suppliers", stopwatch.ElapsedMilliseconds, correlationId,
                new { totalCount, page, pageSize, resultCount = suppliers.Count });

            _logger.LogDatabaseOperation(LogLevel.Information, "get_suppliers_completed", "supplier",
                correlationId: correlationId,
                additionalData: new { totalCount, page, pageSize, resultCount = suppliers.Count });

            return new PaginatedResult<SupplierDto>
            {
                Data = suppliers,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "get_suppliers_failed", "supplier",
                correlationId: correlationId, exception: ex,
                additionalData: new { page, pageSize, search, country, minRating, isActive, durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "get_supplier_by_id_started", "supplier",
                entityId: id, correlationId: correlationId);

            var result = await _context.Suppliers
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

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_get_supplier_by_id", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, found = result != null });

            if (result != null)
            {
                _logger.LogDatabaseOperation(LogLevel.Information, "get_supplier_by_id_completed", "supplier",
                    entityId: id, correlationId: correlationId,
                    additionalData: new { supplierCode = result.SupplierCode, companyName = result.CompanyName });
            }
            else
            {
                _logger.LogDatabaseOperation(LogLevel.Warning, "get_supplier_by_id_not_found", "supplier",
                    entityId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "get_supplier_by_id_failed", "supplier",
                entityId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<Supplier?> GetSupplierEntityByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "get_supplier_entity_by_id_started", "supplier",
                entityId: id, correlationId: correlationId);

            var result = await _context.Suppliers.FindAsync(id);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_get_supplier_entity_by_id", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, found = result != null });

            if (result != null)
            {
                _logger.LogDatabaseOperation(LogLevel.Information, "get_supplier_entity_by_id_completed", "supplier",
                    entityId: id, correlationId: correlationId,
                    additionalData: new { supplierCode = result.SupplierCode, companyName = result.CompanyName });
            }
            else
            {
                _logger.LogDatabaseOperation(LogLevel.Warning, "get_supplier_entity_by_id_not_found", "supplier",
                    entityId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "get_supplier_entity_by_id_failed", "supplier",
                entityId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "create_supplier_started", "supplier",
                correlationId: correlationId,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName });

            supplier.CreatedAt = DateTime.UtcNow;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_create_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = supplier.SupplierId, supplierCode = supplier.SupplierCode });

            _logger.LogDatabaseOperation(LogLevel.Information, "create_supplier_completed", "supplier",
                entityId: supplier.SupplierId, correlationId: correlationId,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName });

            return supplier;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "create_supplier_failed", "supplier",
                correlationId: correlationId, exception: ex,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName, durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "update_supplier_started", "supplier",
                entityId: supplier.SupplierId, correlationId: correlationId,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName });

            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_update_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = supplier.SupplierId, supplierCode = supplier.SupplierCode });

            _logger.LogDatabaseOperation(LogLevel.Information, "update_supplier_completed", "supplier",
                entityId: supplier.SupplierId, correlationId: correlationId,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName });

            return supplier;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "update_supplier_failed", "supplier",
                entityId: supplier.SupplierId, correlationId: correlationId, exception: ex,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName, durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "delete_supplier_started", "supplier",
                entityId: id, correlationId: correlationId);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                stopwatch.Stop();
                _logger.LogDatabaseOperation(LogLevel.Warning, "delete_supplier_not_found", "supplier",
                    entityId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
                return false;
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_delete_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, supplierCode = supplier.SupplierCode });

            _logger.LogDatabaseOperation(LogLevel.Information, "delete_supplier_completed", "supplier",
                entityId: id, correlationId: correlationId,
                additionalData: new { supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName });

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "delete_supplier_failed", "supplier",
                entityId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<bool> SupplierExistsAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Debug, "supplier_exists_check_started", "supplier",
                entityId: id, correlationId: correlationId);

            var result = await _context.Suppliers.AnyAsync(s => s.SupplierId == id);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_supplier_exists_check", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, exists = result });

            _logger.LogDatabaseOperation(LogLevel.Debug, "supplier_exists_check_completed", "supplier",
                entityId: id, correlationId: correlationId, additionalData: new { exists = result });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "supplier_exists_check_failed", "supplier",
                entityId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<bool> SupplierCodeExistsAsync(string supplierCode, int? excludeId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Debug, "supplier_code_exists_check_started", "supplier",
                correlationId: correlationId,
                additionalData: new { supplierCode, excludeId });

            var query = _context.Suppliers.AsNoTracking().Where(s => s.SupplierCode == supplierCode);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.SupplierId != excludeId.Value);
            }

            var result = await query.AnyAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_supplier_code_exists_check", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierCode, excludeId, exists = result });

            _logger.LogDatabaseOperation(LogLevel.Debug, "supplier_code_exists_check_completed", "supplier",
                correlationId: correlationId, additionalData: new { supplierCode, excludeId, exists = result });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "supplier_code_exists_check_failed", "supplier",
                correlationId: correlationId, exception: ex,
                additionalData: new { supplierCode, excludeId, durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Information, "get_countries_started", "supplier",
                correlationId: correlationId);

            var result = await _context.Suppliers
                .AsNoTracking()
                .Where(s => !string.IsNullOrEmpty(s.Country))
                .Select(s => s.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_get_countries", stopwatch.ElapsedMilliseconds, correlationId,
                new { countryCount = result.Count });

            _logger.LogDatabaseOperation(LogLevel.Information, "get_countries_completed", "supplier",
                correlationId: correlationId, additionalData: new { countryCount = result.Count });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "get_countries_failed", "supplier",
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<int> GetTotalSuppliersCountAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDatabaseOperation(LogLevel.Debug, "get_total_suppliers_count_started", "supplier",
                correlationId: correlationId);

            var result = await _context.Suppliers.CountAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("database_get_total_suppliers_count", stopwatch.ElapsedMilliseconds, correlationId,
                new { totalCount = result });

            _logger.LogDatabaseOperation(LogLevel.Debug, "get_total_suppliers_count_completed", "supplier",
                correlationId: correlationId, additionalData: new { totalCount = result });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogDatabaseOperation(LogLevel.Error, "get_total_suppliers_count_failed", "supplier",
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }
}