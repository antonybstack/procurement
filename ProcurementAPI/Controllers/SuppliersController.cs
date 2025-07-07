using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProcurementAPI.DTOs;
using ProcurementAPI.Extensions;
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
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "get_suppliers_started",
                correlationId: correlationId,
                additionalData: new { page, pageSize, search, country, minRating, isActive });

            var result = await _supplierService.GetSuppliersAsync(page, pageSize, search, country, minRating, isActive);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("get_suppliers", stopwatch.ElapsedMilliseconds, correlationId,
                new { result.TotalCount, result.Page, result.PageSize, result.TotalPages });

            _logger.LogSupplierOperation(LogLevel.Information, "get_suppliers_completed",
                correlationId: correlationId,
                additionalData: new { totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize });

            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "get_suppliers_failed",
                correlationId: correlationId,
                exception: ex,
                additionalData: new { page, pageSize, search, country, minRating, isActive, durationMs = stopwatch.ElapsedMilliseconds });

            return StatusCode(500, "An error occurred while retrieving suppliers");
        }
    }

    /// <summary>
    /// Get a specific supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "get_supplier_by_id_started",
                supplierId: id, correlationId: correlationId);

            var supplier = await _supplierService.GetSupplierByIdAsync(id);

            if (supplier == null)
            {
                stopwatch.Stop();
                _logger.LogSupplierOperation(LogLevel.Warning, "get_supplier_by_id_not_found",
                    supplierId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

                return NotFound($"Supplier with ID {id} not found");
            }

            stopwatch.Stop();
            _logger.LogPerformanceMetric("get_supplier_by_id", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, supplierCode = supplier.SupplierCode, companyName = supplier.CompanyName });

            _logger.LogSupplierOperation(LogLevel.Information, "get_supplier_by_id_completed",
                supplierId: id, supplierCode: supplier.SupplierCode, companyName: supplier.CompanyName,
                correlationId: correlationId);

            return Ok(supplier);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "get_supplier_by_id_failed",
                supplierId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return StatusCode(500, "An error occurred while retrieving the supplier");
        }
    }

    /// <summary>
    /// Create a new supplier
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] SupplierUpdateDto createDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "create_supplier_started",
                supplierCode: createDto.SupplierCode, companyName: createDto.CompanyName,
                correlationId: correlationId, additionalData: new { country = createDto.Country, isActive = createDto.IsActive });

            var supplier = await _supplierService.CreateSupplierAsync(createDto);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("create_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = supplier.SupplierId, supplierCode = supplier.SupplierCode });

            _logger.LogSupplierOperation(LogLevel.Information, "create_supplier_completed",
                supplierId: supplier.SupplierId, supplierCode: supplier.SupplierCode,
                companyName: supplier.CompanyName, correlationId: correlationId);

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Warning, "create_supplier_validation_failed",
                supplierCode: createDto.SupplierCode, companyName: createDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Warning, "create_supplier_invalid_data",
                supplierCode: createDto.SupplierCode, companyName: createDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "create_supplier_failed",
                supplierCode: createDto.SupplierCode, companyName: createDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return StatusCode(500, "An error occurred while creating the supplier");
        }
    }

    /// <summary>
    /// Update a supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(int id, [FromBody] SupplierUpdateDto updateDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "update_supplier_started",
                supplierId: id, supplierCode: updateDto.SupplierCode, companyName: updateDto.CompanyName,
                correlationId: correlationId, additionalData: new { country = updateDto.Country, isActive = updateDto.IsActive });

            var supplier = await _supplierService.UpdateSupplierAsync(id, updateDto);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("update_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, supplierCode = supplier.SupplierCode });

            _logger.LogSupplierOperation(LogLevel.Information, "update_supplier_completed",
                supplierId: supplier.SupplierId, supplierCode: supplier.SupplierCode,
                companyName: supplier.CompanyName, correlationId: correlationId);

            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Warning, "update_supplier_validation_failed",
                supplierId: id, supplierCode: updateDto.SupplierCode, companyName: updateDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Warning, "update_supplier_invalid_data",
                supplierId: id, supplierCode: updateDto.SupplierCode, companyName: updateDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "update_supplier_failed",
                supplierId: id, supplierCode: updateDto.SupplierCode, companyName: updateDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

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
            _logger.LogSupplierOperation(LogLevel.Information, "delete_supplier_started",
                supplierId: id, correlationId: correlationId);

            var result = await _supplierService.DeleteSupplierAsync(id);

            if (!result)
            {
                stopwatch.Stop();
                _logger.LogSupplierOperation(LogLevel.Warning, "delete_supplier_not_found",
                    supplierId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

                return NotFound($"Supplier with ID {id} not found");
            }

            stopwatch.Stop();
            _logger.LogPerformanceMetric("delete_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id });

            _logger.LogSupplierOperation(LogLevel.Information, "delete_supplier_completed",
                supplierId: id, correlationId: correlationId);

            return NoContent();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "delete_supplier_failed",
                supplierId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

            return StatusCode(500, "An error occurred while deleting the supplier");
        }
    }

    /// <summary>
    /// Get countries for filtering
    /// </summary>
    [HttpGet("countries")]
    public async Task<ActionResult<List<string>>> GetCountries()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "get_countries_started",
                correlationId: correlationId);

            var countries = await _supplierService.GetCountriesAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("get_countries", stopwatch.ElapsedMilliseconds, correlationId,
                new { countryCount = countries.Count });

            _logger.LogSupplierOperation(LogLevel.Information, "get_countries_completed",
                correlationId: correlationId, additionalData: new { countryCount = countries.Count });

            return Ok(countries);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "get_countries_failed",
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });

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
                _logger.LogValidationError("supplierCode", supplierCode ?? "null", "Supplier code is required",
                    correlationId: correlationId);
                return BadRequest("Supplier code is required");
            }

            _logger.LogSupplierOperation(LogLevel.Information, "validate_supplier_code_started",
                supplierCode: supplierCode, correlationId: correlationId,
                additionalData: new { excludeId });

            var isValid = await _supplierService.ValidateSupplierCodeAsync(supplierCode, excludeId);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("validate_supplier_code", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierCode, excludeId, isValid });

            _logger.LogSupplierOperation(LogLevel.Information, "validate_supplier_code_completed",
                supplierCode: supplierCode, correlationId: correlationId,
                additionalData: new { excludeId, isValid });

            return Ok(isValid);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "validate_supplier_code_failed",
                supplierCode: supplierCode, correlationId: correlationId, exception: ex,
                additionalData: new { excludeId, durationMs = stopwatch.ElapsedMilliseconds });

            return StatusCode(500, "An error occurred while validating the supplier code");
        }
    }
}