using System.Diagnostics;
using System.Net.Mail;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;
using ProcurementAPI.Services.DataServices;

namespace ProcurementAPI.Services;

public class SupplierService : ISupplierService
{
    private readonly ILogger<SupplierService> _logger;
    private readonly ISupplierDataService _supplierDataService;

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
        using var activity = new Activity("SupplierService.GetSuppliersAsync").Start();
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
            _logger.LogInformation("Service get suppliers started - CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, Search: {Search}, Country: {Country}, MinRating: {MinRating}, IsActive: {IsActive}",
                correlationId, page, pageSize, search, country, minRating, isActive);

            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

            if (pageSize > 100)
                _logger.LogWarning("Validation error - PageSize: {PageSize}, Message: Page size exceeds maximum limit of 100, CorrelationId: {CorrelationId}",
                    pageSize, correlationId);

            PaginatedResult<SupplierDto> result = await _supplierDataService.GetSuppliersAsync(page, pageSize, search, country, minRating, isActive);

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Service get suppliers completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, TotalCount: {TotalCount}, Page: {Page}, PageSize: {PageSize}, TotalPages: {TotalPages}",
                stopwatch.ElapsedMilliseconds, correlationId, result.TotalCount, result.Page, result.PageSize, result.TotalPages);

            _logger.LogInformation("Service get suppliers completed - CorrelationId: {CorrelationId}, TotalCount: {TotalCount}, Page: {Page}, PageSize: {PageSize}",
                correlationId, result.TotalCount, result.Page, result.PageSize);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Service get suppliers failed - CorrelationId: {CorrelationId}, Page: {Page}, PageSize: {PageSize}, Search: {Search}, Country: {Country}, MinRating: {MinRating}, IsActive: {IsActive}, DurationMs: {DurationMs}",
                correlationId, page, pageSize, search, country, minRating, isActive, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Service get supplier by ID started - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}", id, correlationId);

            if (id <= 0)
            {
                _logger.LogWarning("Validation error - SupplierId: {SupplierId}, Message: Invalid supplier ID requested, CorrelationId: {CorrelationId}", id, correlationId);
                return null;
            }

            var result = await _supplierDataService.GetSupplierByIdAsync(id);

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Service get supplier by ID completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, Found: {Found}",
                stopwatch.ElapsedMilliseconds, correlationId, id, result != null);

            if (result != null)
                _logger.LogInformation("Service get supplier by ID completed - SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, CorrelationId: {CorrelationId}",
                    id, result.SupplierCode, result.CompanyName, correlationId);
            else
                _logger.LogWarning("Service get supplier by ID not found - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                    id, correlationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service get supplier by ID failed - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                id, correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<SupplierDto> CreateSupplierAsync(SupplierUpdateDto createDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Service create supplier started - SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, Country: {Country}, IsActive: {IsActive}, CorrelationId: {CorrelationId}",
                createDto.SupplierCode, createDto.CompanyName, createDto.Country, createDto.IsActive, correlationId);

            // Validate supplier code uniqueness
            if (await _supplierDataService.SupplierCodeExistsAsync(createDto.SupplierCode))
            {
                _logger.LogWarning("Validation error - SupplierCode: {SupplierCode}, Message: Supplier code already exists, CorrelationId: {CorrelationId}",
                    createDto.SupplierCode, correlationId);
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
            _logger.LogInformation("Performance metric - Service create supplier completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, SupplierCode: {SupplierCode}",
                stopwatch.ElapsedMilliseconds, correlationId, createdSupplier.SupplierId, createdSupplier.SupplierCode);

            _logger.LogInformation("Service create supplier completed - SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, CorrelationId: {CorrelationId}",
                createdSupplier.SupplierId, createdSupplier.SupplierCode, createdSupplier.CompanyName, correlationId);

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
            _logger.LogError(ex, "Service create supplier failed - SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                createDto.SupplierCode, createDto.CompanyName, correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<SupplierDto> UpdateSupplierAsync(int id, SupplierUpdateDto updateDto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";


        try
        {
            _logger.LogInformation("Service update supplier started - SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, Country: {Country}, IsActive: {IsActive}, CorrelationId: {CorrelationId}",
                id, updateDto.SupplierCode, updateDto.CompanyName, updateDto.Country, updateDto.IsActive, correlationId);

            if (id <= 0)
            {
                _logger.LogWarning("Validation error - SupplierId: {SupplierId}, Message: Invalid supplier ID, CorrelationId: {CorrelationId}", id, correlationId);
                throw new ArgumentException("Invalid supplier ID", nameof(id));
            }

            var existingSupplier = await _supplierDataService.GetSupplierEntityByIdAsync(id);

            if (existingSupplier == null)
            {
                _logger.LogWarning("Service update supplier not found - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                    id, correlationId, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Supplier with ID {id} not found.");
            }

            if (await _supplierDataService.SupplierCodeExistsAsync(updateDto.SupplierCode, id))
            {
                _logger.LogWarning("Validation error - SupplierCode: {SupplierCode}, Message: Supplier code already exists, SupplierId: {SupplierId}, CorrelationId: {CorrelationId}",
                    updateDto.SupplierCode, id, correlationId);
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
            _logger.LogInformation("Performance metric - Service update supplier completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, SupplierCode: {SupplierCode}",
                stopwatch.ElapsedMilliseconds, correlationId, id, updatedSupplier.SupplierCode);

            _logger.LogInformation("Service update supplier completed - SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, CorrelationId: {CorrelationId}",
                updatedSupplier.SupplierId, updatedSupplier.SupplierCode, updatedSupplier.CompanyName, correlationId);

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
            _logger.LogError(ex, "Service update supplier failed - SupplierId: {SupplierId}, SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                id, updateDto.SupplierCode, updateDto.CompanyName, correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Service delete supplier started - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}", id, correlationId);

            if (id <= 0)
            {
                _logger.LogWarning("Validation error - SupplierId: {SupplierId}, Message: Invalid supplier ID for deletion, CorrelationId: {CorrelationId}", id, correlationId);
                return false;
            }

            var result = await _supplierDataService.DeleteSupplierAsync(id);

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Service delete supplier completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierId: {SupplierId}, Deleted: {Deleted}",
                stopwatch.ElapsedMilliseconds, correlationId, id, result);

            if (result)
                _logger.LogInformation("Service delete supplier completed - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}", id, correlationId);
            else
                _logger.LogWarning("Service delete supplier not found - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                    id, correlationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service delete supplier failed - SupplierId: {SupplierId}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                id, correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Service get countries started - CorrelationId: {CorrelationId}", correlationId);

            List<string> result = await _supplierDataService.GetCountriesAsync();

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Service get countries completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, CountryCount: {CountryCount}",
                stopwatch.ElapsedMilliseconds, correlationId, result.Count);

            _logger.LogInformation("Service get countries completed - CorrelationId: {CorrelationId}, CountryCount: {CountryCount}", correlationId, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service get countries failed - CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<bool> ValidateSupplierCodeAsync(string supplierCode, int? excludeId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("Service validate supplier code started - SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, CorrelationId: {CorrelationId}",
                supplierCode, excludeId, correlationId);

            if (string.IsNullOrWhiteSpace(supplierCode))
            {
                _logger.LogWarning("Validation error - SupplierCode: {SupplierCode}, Message: Supplier code is null or empty, CorrelationId: {CorrelationId}",
                    supplierCode ?? "null", correlationId);
                return false;
            }

            var result = !await _supplierDataService.SupplierCodeExistsAsync(supplierCode, excludeId);

            stopwatch.Stop();
            _logger.LogInformation("Performance metric - Service validate supplier code completed in {ElapsedMs}ms - CorrelationId: {CorrelationId}, SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, IsValid: {IsValid}",
                stopwatch.ElapsedMilliseconds, correlationId, supplierCode, excludeId, result);

            _logger.LogInformation("Service validate supplier code completed - SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, IsValid: {IsValid}, CorrelationId: {CorrelationId}",
                supplierCode, excludeId, result, correlationId);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service validate supplier code failed - SupplierCode: {SupplierCode}, ExcludeId: {ExcludeId}, CorrelationId: {CorrelationId}, DurationMs: {DurationMs}",
                supplierCode, excludeId, correlationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void ValidateSupplierData(SupplierUpdateDto dto)
    {
        var correlationId = Activity.Current?.Id ?? "unknown";

        if (string.IsNullOrWhiteSpace(dto.SupplierCode))
        {
            _logger.LogWarning("Validation error - SupplierCode: {SupplierCode}, Message: Supplier code is required, CorrelationId: {CorrelationId}",
                dto.SupplierCode ?? "null", correlationId);
            throw new ArgumentException("Supplier code is required", nameof(dto.SupplierCode));
        }

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            _logger.LogWarning("Validation error - CompanyName: {CompanyName}, Message: Company name is required, CorrelationId: {CorrelationId}",
                dto.CompanyName ?? "null", correlationId);
            throw new ArgumentException("Company name is required", nameof(dto.CompanyName));
        }

        if (dto.Rating.HasValue && (dto.Rating < 1 || dto.Rating > 5))
        {
            _logger.LogWarning("Validation error - Rating: {Rating}, Message: Rating must be between 1 and 5, CorrelationId: {CorrelationId}",
                dto.Rating.Value, correlationId);
            throw new ArgumentException("Rating must be between 1 and 5", nameof(dto.Rating));
        }

        if (dto.CreditLimit.HasValue && dto.CreditLimit < 0)
        {
            _logger.LogWarning("Validation error - CreditLimit: {CreditLimit}, Message: Credit limit cannot be negative, CorrelationId: {CorrelationId}",
                dto.CreditLimit.Value, correlationId);
            throw new ArgumentException("Credit limit cannot be negative", nameof(dto.CreditLimit));
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && !IsValidEmail(dto.Email))
        {
            _logger.LogWarning("Validation error - Email: {Email}, Message: Invalid email format, CorrelationId: {CorrelationId}",
                dto.Email, correlationId);
            throw new ArgumentException("Invalid email format", nameof(dto.Email));
        }

        _logger.LogInformation("Supplier validation passed - SupplierCode: {SupplierCode}, CompanyName: {CompanyName}, CorrelationId: {CorrelationId}",
            dto.SupplierCode, dto.CompanyName, correlationId);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}