using System.Diagnostics;
using ProcurementAPI.DTOs;
using ProcurementAPI.Extensions;
using ProcurementAPI.Models;
using ProcurementAPI.Services.DataServices;

namespace ProcurementAPI.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierDataService _supplierDataService;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(ISupplierDataService supplierDataService, ILogger<SupplierService> logger)
    {
        _supplierDataService = supplierDataService;
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
            _logger.LogSupplierOperation(LogLevel.Information, "service_get_suppliers_started",
                correlationId: correlationId,
                additionalData: new { page, pageSize, search, country, minRating, isActive });

            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

            if (pageSize > 100)
            {
                _logger.LogValidationError("pageSize", pageSize.ToString(), "Page size exceeds maximum limit of 100",
                    correlationId: correlationId);
            }

            var result = await _supplierDataService.GetSuppliersAsync(page, pageSize, search, country, minRating, isActive);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_get_suppliers", stopwatch.ElapsedMilliseconds, correlationId,
                new { result.TotalCount, result.Page, result.PageSize, result.TotalPages });

            _logger.LogSupplierOperation(LogLevel.Information, "service_get_suppliers_completed",
                correlationId: correlationId,
                additionalData: new { totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_get_suppliers_failed",
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
            _logger.LogSupplierOperation(LogLevel.Information, "service_get_supplier_by_id_started",
                supplierId: id, correlationId: correlationId);

            if (id <= 0)
            {
                _logger.LogValidationError("supplierId", id.ToString(), "Invalid supplier ID requested",
                    supplierId: id, correlationId: correlationId);
                return null;
            }

            var result = await _supplierDataService.GetSupplierByIdAsync(id);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_get_supplier_by_id", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, found = result != null });

            if (result != null)
            {
                _logger.LogSupplierOperation(LogLevel.Information, "service_get_supplier_by_id_completed",
                    supplierId: id, supplierCode: result.SupplierCode, companyName: result.CompanyName,
                    correlationId: correlationId);
            }
            else
            {
                _logger.LogSupplierOperation(LogLevel.Warning, "service_get_supplier_by_id_not_found",
                    supplierId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_get_supplier_by_id_failed",
                supplierId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<SupplierDto> CreateSupplierAsync(SupplierUpdateDto createDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "service_create_supplier_started",
                supplierCode: createDto.SupplierCode, companyName: createDto.CompanyName,
                correlationId: correlationId, additionalData: new { country = createDto.Country, isActive = createDto.IsActive });

            // Validate supplier code uniqueness
            if (await _supplierDataService.SupplierCodeExistsAsync(createDto.SupplierCode))
            {
                _logger.LogValidationError("supplierCode", createDto.SupplierCode, "Supplier code already exists",
                    correlationId: correlationId);
                throw new InvalidOperationException($"Supplier code '{createDto.SupplierCode}' already exists.");
            }

            // Validate business rules
            ValidateSupplierData(createDto);

            var supplier = new Supplier
            {
                SupplierCode = createDto.SupplierCode,
                CompanyName = createDto.CompanyName,
                ContactName = createDto.ContactName,
                Email = createDto.Email,
                Phone = createDto.Phone,
                Address = createDto.Address,
                City = createDto.City,
                State = createDto.State,
                Country = createDto.Country,
                PostalCode = createDto.PostalCode,
                TaxId = createDto.TaxId,
                PaymentTerms = createDto.PaymentTerms,
                CreditLimit = createDto.CreditLimit,
                Rating = createDto.Rating,
                IsActive = createDto.IsActive
            };

            var createdSupplier = await _supplierDataService.CreateSupplierAsync(supplier);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_create_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = createdSupplier.SupplierId, supplierCode = createdSupplier.SupplierCode });

            _logger.LogSupplierOperation(LogLevel.Information, "service_create_supplier_completed",
                supplierId: createdSupplier.SupplierId, supplierCode: createdSupplier.SupplierCode,
                companyName: createdSupplier.CompanyName, correlationId: correlationId);

            return new SupplierDto
            {
                SupplierId = createdSupplier.SupplierId,
                SupplierCode = createdSupplier.SupplierCode,
                CompanyName = createdSupplier.CompanyName,
                ContactName = createdSupplier.ContactName,
                Email = createdSupplier.Email,
                Phone = createdSupplier.Phone,
                Address = createdSupplier.Address,
                City = createdSupplier.City,
                State = createdSupplier.State,
                Country = createdSupplier.Country,
                PostalCode = createdSupplier.PostalCode,
                TaxId = createdSupplier.TaxId,
                PaymentTerms = createdSupplier.PaymentTerms,
                CreditLimit = createdSupplier.CreditLimit,
                Rating = createdSupplier.Rating,
                IsActive = createdSupplier.IsActive,
                CreatedAt = createdSupplier.CreatedAt
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_create_supplier_failed",
                supplierCode: createDto.SupplierCode, companyName: createDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<SupplierDto> UpdateSupplierAsync(int id, SupplierUpdateDto updateDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "service_update_supplier_started",
                supplierId: id, supplierCode: updateDto.SupplierCode, companyName: updateDto.CompanyName,
                correlationId: correlationId, additionalData: new { country = updateDto.Country, isActive = updateDto.IsActive });

            if (id <= 0)
            {
                _logger.LogValidationError("supplierId", id.ToString(), "Invalid supplier ID",
                    supplierId: id, correlationId: correlationId);
                throw new ArgumentException("Invalid supplier ID", nameof(id));
            }

            // Check if supplier exists
            var existingSupplier = await _supplierDataService.GetSupplierEntityByIdAsync(id);
            if (existingSupplier == null)
            {
                _logger.LogSupplierOperation(LogLevel.Warning, "service_update_supplier_not_found",
                    supplierId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
                throw new InvalidOperationException($"Supplier with ID {id} not found.");
            }

            // Validate supplier code uniqueness (excluding current supplier)
            if (await _supplierDataService.SupplierCodeExistsAsync(updateDto.SupplierCode, id))
            {
                _logger.LogValidationError("supplierCode", updateDto.SupplierCode, "Supplier code already exists",
                    supplierId: id, correlationId: correlationId);
                throw new InvalidOperationException($"Supplier code '{updateDto.SupplierCode}' already exists.");
            }

            // Validate business rules
            ValidateSupplierData(updateDto);

            // Update supplier properties
            existingSupplier.SupplierCode = updateDto.SupplierCode;
            existingSupplier.CompanyName = updateDto.CompanyName;
            existingSupplier.ContactName = updateDto.ContactName;
            existingSupplier.Email = updateDto.Email;
            existingSupplier.Phone = updateDto.Phone;
            existingSupplier.Address = updateDto.Address;
            existingSupplier.City = updateDto.City;
            existingSupplier.State = updateDto.State;
            existingSupplier.Country = updateDto.Country;
            existingSupplier.PostalCode = updateDto.PostalCode;
            existingSupplier.TaxId = updateDto.TaxId;
            existingSupplier.PaymentTerms = updateDto.PaymentTerms;
            existingSupplier.CreditLimit = updateDto.CreditLimit;
            existingSupplier.Rating = updateDto.Rating;
            existingSupplier.IsActive = updateDto.IsActive;

            var updatedSupplier = await _supplierDataService.UpdateSupplierAsync(existingSupplier);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_update_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, supplierCode = updatedSupplier.SupplierCode });

            _logger.LogSupplierOperation(LogLevel.Information, "service_update_supplier_completed",
                supplierId: updatedSupplier.SupplierId, supplierCode: updatedSupplier.SupplierCode,
                companyName: updatedSupplier.CompanyName, correlationId: correlationId);

            return new SupplierDto
            {
                SupplierId = updatedSupplier.SupplierId,
                SupplierCode = updatedSupplier.SupplierCode,
                CompanyName = updatedSupplier.CompanyName,
                ContactName = updatedSupplier.ContactName,
                Email = updatedSupplier.Email,
                Phone = updatedSupplier.Phone,
                Address = updatedSupplier.Address,
                City = updatedSupplier.City,
                State = updatedSupplier.State,
                Country = updatedSupplier.Country,
                PostalCode = updatedSupplier.PostalCode,
                TaxId = updatedSupplier.TaxId,
                PaymentTerms = updatedSupplier.PaymentTerms,
                CreditLimit = updatedSupplier.CreditLimit,
                Rating = updatedSupplier.Rating,
                IsActive = updatedSupplier.IsActive,
                CreatedAt = updatedSupplier.CreatedAt
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_update_supplier_failed",
                supplierId: id, supplierCode: updateDto.SupplierCode, companyName: updateDto.CompanyName,
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "service_delete_supplier_started",
                supplierId: id, correlationId: correlationId);

            if (id <= 0)
            {
                _logger.LogValidationError("supplierId", id.ToString(), "Invalid supplier ID for deletion",
                    supplierId: id, correlationId: correlationId);
                return false;
            }

            var result = await _supplierDataService.DeleteSupplierAsync(id);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_delete_supplier", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierId = id, deleted = result });

            if (result)
            {
                _logger.LogSupplierOperation(LogLevel.Information, "service_delete_supplier_completed",
                    supplierId: id, correlationId: correlationId);
            }
            else
            {
                _logger.LogSupplierOperation(LogLevel.Warning, "service_delete_supplier_not_found",
                    supplierId: id, correlationId: correlationId,
                    additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_delete_supplier_failed",
                supplierId: id, correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "service_get_countries_started",
                correlationId: correlationId);

            var result = await _supplierDataService.GetCountriesAsync();

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_get_countries", stopwatch.ElapsedMilliseconds, correlationId,
                new { countryCount = result.Count });

            _logger.LogSupplierOperation(LogLevel.Information, "service_get_countries_completed",
                correlationId: correlationId, additionalData: new { countryCount = result.Count });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_get_countries_failed",
                correlationId: correlationId, exception: ex,
                additionalData: new { durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    public async Task<bool> ValidateSupplierCodeAsync(string supplierCode, int? excludeId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogSupplierOperation(LogLevel.Information, "service_validate_supplier_code_started",
                supplierCode: supplierCode, correlationId: correlationId,
                additionalData: new { excludeId });

            if (string.IsNullOrWhiteSpace(supplierCode))
            {
                _logger.LogValidationError("supplierCode", supplierCode ?? "null", "Supplier code is null or empty",
                    correlationId: correlationId);
                return false;
            }

            var result = !await _supplierDataService.SupplierCodeExistsAsync(supplierCode, excludeId);

            stopwatch.Stop();
            _logger.LogPerformanceMetric("service_validate_supplier_code", stopwatch.ElapsedMilliseconds, correlationId,
                new { supplierCode, excludeId, isValid = result });

            _logger.LogSupplierOperation(LogLevel.Information, "service_validate_supplier_code_completed",
                supplierCode: supplierCode, correlationId: correlationId,
                additionalData: new { excludeId, isValid = result });

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogSupplierOperation(LogLevel.Error, "service_validate_supplier_code_failed",
                supplierCode: supplierCode, correlationId: correlationId, exception: ex,
                additionalData: new { excludeId, durationMs = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    private void ValidateSupplierData(SupplierUpdateDto dto)
    {
        var correlationId = Activity.Current?.Id ?? "unknown";

        if (string.IsNullOrWhiteSpace(dto.SupplierCode))
        {
            _logger.LogValidationError("supplierCode", dto.SupplierCode ?? "null", "Supplier code is required",
                correlationId: correlationId);
            throw new ArgumentException("Supplier code is required", nameof(dto.SupplierCode));
        }

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            _logger.LogValidationError("companyName", dto.CompanyName ?? "null", "Company name is required",
                correlationId: correlationId);
            throw new ArgumentException("Company name is required", nameof(dto.CompanyName));
        }

        if (dto.Rating.HasValue && (dto.Rating < 1 || dto.Rating > 5))
        {
            _logger.LogValidationError("rating", dto.Rating.Value.ToString(), "Rating must be between 1 and 5",
                correlationId: correlationId);
            throw new ArgumentException("Rating must be between 1 and 5", nameof(dto.Rating));
        }

        if (dto.CreditLimit.HasValue && dto.CreditLimit < 0)
        {
            _logger.LogValidationError("creditLimit", dto.CreditLimit.Value.ToString(), "Credit limit cannot be negative",
                correlationId: correlationId);
            throw new ArgumentException("Credit limit cannot be negative", nameof(dto.CreditLimit));
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && !IsValidEmail(dto.Email))
        {
            _logger.LogValidationError("email", dto.Email, "Invalid email format",
                correlationId: correlationId);
            throw new ArgumentException("Invalid email format", nameof(dto.Email));
        }

        _logger.LogSupplierOperation(LogLevel.Information, "supplier_validation_passed",
            supplierCode: dto.SupplierCode, companyName: dto.CompanyName,
            correlationId: correlationId);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}