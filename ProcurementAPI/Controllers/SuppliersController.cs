using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ProcurementDbContext _context;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ProcurementDbContext context, ILogger<SuppliersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all suppliers with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SupplierDto>>> GetSuppliers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? country = null,
        [FromQuery] int? minRating = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Suppliers.AsQueryable();

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

            // Apply pagination
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
                    City = s.City,
                    State = s.State,
                    Country = s.Country,
                    Rating = s.Rating,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            var result = new PaginatedResult<SupplierDto>
            {
                Data = suppliers,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suppliers");
            return StatusCode(500, "An error occurred while retrieving suppliers");
        }
    }

    /// <summary>
    /// Get a specific supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
    {
        try
        {
            var supplier = await _context.Suppliers
                .Where(s => s.SupplierId == id)
                .Select(s => new SupplierDto
                {
                    SupplierId = s.SupplierId,
                    SupplierCode = s.SupplierCode,
                    CompanyName = s.CompanyName,
                    ContactName = s.ContactName,
                    Email = s.Email,
                    Phone = s.Phone,
                    City = s.City,
                    State = s.State,
                    Country = s.Country,
                    Rating = s.Rating,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (supplier == null)
            {
                return NotFound($"Supplier with ID {id} not found");
            }

            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier with ID {SupplierId}", id);
            return StatusCode(500, "An error occurred while retrieving the supplier");
        }
    }

    /// <summary>
    /// Get supplier performance summary
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<List<SupplierSummaryDto>>> GetSupplierPerformance(
        [FromQuery] int? top = null)
    {
        try
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive)
                .Include(s => s.Quotes)
                .Include(s => s.PurchaseOrders)
                .ToListAsync();

            var result = suppliers
                .Select(s => new SupplierSummaryDto
                {
                    SupplierId = s.SupplierId,
                    CompanyName = s.CompanyName,
                    Rating = s.Rating,
                    TotalQuotes = s.Quotes.Count,
                    AwardedQuotes = s.Quotes.Count(q => q.Status == QuoteStatus.Awarded),
                    AvgQuotePrice = s.Quotes.Any() ? s.Quotes.Average(q => q.UnitPrice) : null,
                    TotalPurchaseOrders = s.PurchaseOrders.Count,
                    TotalPoValue = s.PurchaseOrders.Any() ? s.PurchaseOrders.Sum(po => po.TotalAmount) : null
                })
                .OrderByDescending(s => s.AwardedQuotes)
                .ThenByDescending(s => s.TotalPoValue)
                .ToList();

            if (top.HasValue)
            {
                result = result.Take(top.Value).ToList();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier performance");
            return StatusCode(500, "An error occurred while retrieving supplier performance");
        }
    }

    /// <summary>
    /// Get countries for filtering
    /// </summary>
    [HttpGet("countries")]
    public async Task<ActionResult<List<string>>> GetCountries()
    {
        try
        {
            var countries = await _context.Suppliers
                .Where(s => !string.IsNullOrEmpty(s.Country))
                .Select(s => s.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(countries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving countries");
            return StatusCode(500, "An error occurred while retrieving countries");
        }
    }
}

public class PaginatedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}