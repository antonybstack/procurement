using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProcurementAPI.DTOs;
using ProcurementAPI.Services;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger)
    {
        _supplierService = supplierService;
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
        using var activity = new Activity("SuppliersController.GetSuppliers").Start();
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);
        activity?.SetTag("search", search);
        activity?.SetTag("country", country);
        activity?.SetTag("minRating", minRating);
        activity?.SetTag("isActive", isActive);
        try
        {
            var result = await _supplierService.GetSuppliersAsync(page, pageSize, search, country, minRating, isActive);
            return Ok(result);
        }
        catch (Exception ex)
        {
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
            var supplier = await _supplierService.GetSupplierByIdAsync(id);

            if (supplier == null)
            {
                return NotFound($"Supplier with ID {id} not found");
            }

            return Ok(supplier);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while retrieving the supplier");
        }
    }

    /// <summary>
    /// Create a new supplier
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] SupplierUpdateDto createDto)
    {
        try
        {
            var supplier = await _supplierService.CreateSupplierAsync(createDto);

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {

            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while creating the supplier");
        }
    }

    /// <summary>
    /// Update a supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(int id, [FromBody] SupplierUpdateDto updateDto)
    {
        try
        {
            var supplier = await _supplierService.UpdateSupplierAsync(id, updateDto);

            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while updating the supplier");
        }
    }

    /// <summary>
    /// Delete a supplier
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSupplier(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            var result = await _supplierService.DeleteSupplierAsync(id);

            if (!result)
            {
                return NotFound($"Supplier with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return StatusCode(500, "An error occurred while deleting the supplier");
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
            var countries = await _supplierService.GetCountriesAsync();

            return Ok(countries);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while retrieving countries");
        }
    }

    /// <summary>
    /// Validate if a supplier code is available
    /// </summary>
    [HttpGet("validate-code")]
    public async Task<ActionResult<bool>> ValidateSupplierCode(
        [FromQuery] string supplierCode,
        [FromQuery] int? excludeId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            if (string.IsNullOrWhiteSpace(supplierCode))
            {
                return BadRequest("Supplier code is required");
            }

            var isValid = await _supplierService.ValidateSupplierCodeAsync(supplierCode, excludeId);

            stopwatch.Stop();

            return Ok(isValid);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return StatusCode(500, "An error occurred while validating the supplier code");
        }
    }
}