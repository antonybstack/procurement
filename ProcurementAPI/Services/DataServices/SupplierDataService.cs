using System.Diagnostics;
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
        using var activity = new Activity("SupplierDataService.GetSuppliersAsync").Start();
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);
        activity?.SetTag("search", search);
        activity?.SetTag("country", country);
        activity?.SetTag("minRating", minRating);
        activity?.SetTag("isActive", isActive);
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Get suppliers started - EntityType: supplier, CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, Search: {Search}, Country: {Country}, MinRating: {MinRating}, IsActive: {IsActive}",
                correlationId, page, pageSize, search, country, minRating, isActive);

            var query = _context.Suppliers.AsNoTracking().AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.CompanyName.Contains(search) ||
                    s.SupplierCode.Contains(search) ||
                    s.ContactName!.Contains(search));

                _logger.LogDebug("Database operation - Applied search filter - EntityType: supplier, CorrelationId: {CorrelationId}, Search: {Search}", correlationId, search);
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(s => s.Country == country);
                _logger.LogDebug("Database operation - Applied country filter - EntityType: supplier, CorrelationId: {CorrelationId}, Country: {Country}", correlationId, country);
            }

            if (minRating.HasValue)
            {
                query = query.Where(s => s.Rating >= minRating.Value);
                _logger.LogDebug("Database operation - Applied rating filter - EntityType: supplier, CorrelationId: {CorrelationId}, MinRating: {MinRating}", correlationId, minRating);
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
                _logger.LogDebug("Database operation - Applied active filter - EntityType: supplier, CorrelationId: {CorrelationId}, IsActive: {IsActive}", correlationId, isActive);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            _logger.LogDebug("Database operation - Count query executed - EntityType: supplier, CorrelationId: {CorrelationId}, TotalCount: {TotalCount}", correlationId, totalCount);

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
            _logger.LogInformation("Performance metric - Database get suppliers completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, TotalCount: {TotalCount}, Page: {Page}, PageSize: {PageSize}, ResultCount: {ResultCount}",
                stopwatch.ElapsedMilliseconds, correlationId, totalCount, page, pageSize, suppliers.Count);

            _logger.LogInformation("Database operation - Get suppliers completed - EntityType: supplier, CorrelationId: {CorrelationId}, TotalCount: {TotalCount}, Page: {Page}, PageSize: {PageSize}, ResultCount: {ResultCount}",
                correlationId, totalCount, page, pageSize, suppliers.Count);

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
            _logger.LogError(ex, "Database operation - Get suppliers failed - EntityType: supplier, CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, Search: {Search}, Country: {Country}, MinRating: {MinRating}, IsActive: {IsActive}, DurationMs: {DurationMs}",
                correlationId, page, pageSize, search, country, minRating, isActive, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<SupplierDetailDto?> GetSupplierByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Get supplier by ID started - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, id);

            // 1. Get the core supplier details and capabilities
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.SupplierCapabilities) // Eagerly load capabilities
                .Where(s => s.SupplierId == id)
                .Select(s => new SupplierDetailDto
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
                    CreatedAt = s.CreatedAt,
                    Capabilities = s.SupplierCapabilities.Select(sc => new SupplierCapabilityDto
                    {
                        CapabilityId = sc.CapabilityId,
                        SupplierId = sc.SupplierId,
                        CapabilityType = sc.CapabilityType,
                        CapabilityValue = sc.CapabilityValue,
                        CreatedAt = sc.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (supplier == null)
            {
                _logger.LogWarning("Database operation - Get supplier by ID not found - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                    correlationId, id);
                return null;
            }

            // 2. Get the performance data from the view
            var performanceData = await _context.SupplierPerformance
                 .FromSqlRaw("SELECT * FROM supplier_performance WHERE supplier_id = {0}", id)
                 .Select(sp => new SupplierPerformanceDataDto
                 {
                     TotalQuotes = sp.TotalQuotes,
                     AwardedQuotes = sp.AwardedQuotes,
                     AvgQuotePrice = sp.AvgQuotePrice,
                     TotalPurchaseOrders = sp.TotalPurchaseOrders
                 })
                 .FirstOrDefaultAsync();

            // 3. Combine the data
            supplier.Performance = performanceData;

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database get supplier by ID completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, Found: {Found}",
                stopwatch.ElapsedMilliseconds, correlationId, id, true);

            return supplier;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Get supplier by ID failed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, DurationMs: {DurationMs}",
                correlationId, id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Supplier?> GetSupplierEntityByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Get supplier entity by ID started - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, id);

            var result = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database get supplier entity by ID completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, Found: {Found}",
                stopwatch.ElapsedMilliseconds, correlationId, id, result != null);

            if (result != null)
            {
                _logger.LogInformation("Database operation - Get supplier entity by ID completed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                    correlationId, id);
            }
            else
            {
                _logger.LogWarning("Database operation - Get supplier entity by ID not found - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                    correlationId, id);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Get supplier entity by ID failed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, DurationMs: {DurationMs}",
                correlationId, id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Create supplier started - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}",
                correlationId, supplier.SupplierCode, supplier.CompanyName);

            supplier.CreatedAt = DateTime.UtcNow;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database create supplier completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                stopwatch.ElapsedMilliseconds, correlationId, supplier.SupplierId);

            _logger.LogInformation("Database operation - Create supplier completed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, supplier.SupplierId);

            return supplier;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Create supplier failed - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, DurationMs: {DurationMs}",
                correlationId, supplier.SupplierCode, supplier.CompanyName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Update supplier started - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}",
                correlationId, supplier.SupplierId, supplier.SupplierCode, supplier.CompanyName);

            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database operation - Update supplier completed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, supplier.SupplierId);

            return supplier;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Update supplier failed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, DurationMs: {DurationMs}",
                correlationId, supplier.SupplierId, supplier.SupplierCode, supplier.CompanyName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Delete supplier started - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, id);

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                stopwatch.Stop();
                _logger.LogWarning("Database operation - Delete supplier not found - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                    correlationId, id);
                return false;
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database delete supplier completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                stopwatch.ElapsedMilliseconds, correlationId, id);

            _logger.LogInformation("Database operation - Delete supplier completed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, id);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Delete supplier failed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, DurationMs: {DurationMs}",
                correlationId, id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<bool> SupplierExistsAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDebug("Database operation - Supplier exists check started - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, id);

            var result = await _context.Suppliers.AnyAsync(s => s.SupplierId == id);

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database supplier exists check completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, Exists: {Exists}",
                stopwatch.ElapsedMilliseconds, correlationId, id, result);

            _logger.LogDebug("Database operation - Supplier exists check completed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}",
                correlationId, id);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Supplier exists check failed - EntityType: supplier, CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, DurationMs: {DurationMs}",
                correlationId, id, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<bool> SupplierCodeExistsAsync(string supplierCode, int? excludeId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDebug("Database operation - Supplier code exists check started - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}",
                correlationId, supplierCode, excludeId);

            var query = _context.Suppliers.AsNoTracking().Where(s => s.SupplierCode == supplierCode);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.SupplierId != excludeId.Value);
            }

            var result = await query.AnyAsync();

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database supplier code exists check completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, Exists: {Exists}",
                stopwatch.ElapsedMilliseconds, correlationId, supplierCode, excludeId, result);

            _logger.LogDebug("Database operation - Supplier code exists check completed - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, Exists: {Exists}",
                correlationId, supplierCode, excludeId, result);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Supplier code exists check failed - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, DurationMs: {DurationMs}",
                correlationId, supplierCode, excludeId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Database operation - Get countries started - CorrelationId: {CorrelationId}",
                correlationId);

            var result = await _context.Suppliers
                .AsNoTracking()
                .Where(s => !string.IsNullOrEmpty(s.Country))
                .Select(s => s.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database get countries completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, CountryCount: {CountryCount}",
                stopwatch.ElapsedMilliseconds, correlationId, result.Count);

            _logger.LogInformation("Database operation - Get countries completed - CorrelationId: {CorrelationId}, CountryCount: {CountryCount}",
                correlationId, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Get countries failed - CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<int> GetTotalSuppliersCountAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogDebug("Database operation - Get total suppliers count started - CorrelationId: {CorrelationId}",
                correlationId);

            var result = await _context.Suppliers.CountAsync();

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Database get total suppliers count completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, TotalCount: {TotalCount}",
                stopwatch.ElapsedMilliseconds, correlationId, result);

            _logger.LogDebug("Database operation - Get total suppliers count completed - CorrelationId: {CorrelationId}, TotalCount: {TotalCount}",
                correlationId, result);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database operation - Get total suppliers count failed - CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}