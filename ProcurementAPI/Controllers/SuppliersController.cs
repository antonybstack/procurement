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
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

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
    /// Update a supplier - expects full DTO from client, EF Core change tracker handles updates
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(int id, [FromBody] SupplierUpdateDto updateDto)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound($"Supplier with ID {id} not found");
            }

            // Update all fields from the DTO
            supplier.SupplierCode = updateDto.SupplierCode;
            supplier.CompanyName = updateDto.CompanyName;
            supplier.ContactName = updateDto.ContactName;
            supplier.Email = updateDto.Email;
            supplier.Phone = updateDto.Phone;
            supplier.Address = updateDto.Address;
            supplier.City = updateDto.City;
            supplier.State = updateDto.State;
            supplier.Country = updateDto.Country;
            supplier.PostalCode = updateDto.PostalCode;
            supplier.TaxId = updateDto.TaxId;
            supplier.PaymentTerms = updateDto.PaymentTerms;
            supplier.CreditLimit = updateDto.CreditLimit;
            supplier.Rating = updateDto.Rating;
            supplier.IsActive = updateDto.IsActive;

            // Update the timestamp
            supplier.UpdatedAt = DateTime.UtcNow;

            // EF Core change tracker will automatically detect which fields changed
            await _context.SaveChangesAsync();

            // Return the updated supplier
            var updatedSupplier = new SupplierDto
            {
                SupplierId = supplier.SupplierId,
                SupplierCode = supplier.SupplierCode,
                CompanyName = supplier.CompanyName,
                ContactName = supplier.ContactName,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address,
                City = supplier.City,
                State = supplier.State,
                Country = supplier.Country,
                PostalCode = supplier.PostalCode,
                TaxId = supplier.TaxId,
                PaymentTerms = supplier.PaymentTerms,
                CreditLimit = supplier.CreditLimit,
                Rating = supplier.Rating,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt
            };

            _logger.LogInformation("Supplier {SupplierId} updated successfully", id);
            return Ok(updatedSupplier);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating supplier {SupplierId}", id);
            return Conflict("The supplier was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
            return StatusCode(500, "An error occurred while updating the supplier");
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