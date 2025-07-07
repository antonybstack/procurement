using ProcurementAPI.DTOs;
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
        // Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Cap at 100 to prevent abuse

        return await _supplierDataService.GetSuppliersAsync(page, pageSize, search, country, minRating, isActive);
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid supplier ID requested: {SupplierId}", id);
            return null;
        }

        return await _supplierDataService.GetSupplierByIdAsync(id);
    }

    public async Task<SupplierDto> CreateSupplierAsync(SupplierUpdateDto createDto)
    {
        // Validate supplier code uniqueness
        if (await _supplierDataService.SupplierCodeExistsAsync(createDto.SupplierCode))
        {
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

        _logger.LogInformation("Supplier created successfully: {SupplierId}", createdSupplier.SupplierId);

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

    public async Task<SupplierDto> UpdateSupplierAsync(int id, SupplierUpdateDto updateDto)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Invalid supplier ID", nameof(id));
        }

        // Check if supplier exists
        var existingSupplier = await _supplierDataService.GetSupplierEntityByIdAsync(id);
        if (existingSupplier == null)
        {
            throw new InvalidOperationException($"Supplier with ID {id} not found.");
        }

        // Validate supplier code uniqueness (excluding current supplier)
        if (await _supplierDataService.SupplierCodeExistsAsync(updateDto.SupplierCode, id))
        {
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

        _logger.LogInformation("Supplier updated successfully: {SupplierId}", id);

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

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid supplier ID for deletion: {SupplierId}", id);
            return false;
        }

        var result = await _supplierDataService.DeleteSupplierAsync(id);

        if (result)
        {
            _logger.LogInformation("Supplier deleted successfully: {SupplierId}", id);
        }
        else
        {
            _logger.LogWarning("Supplier not found for deletion: {SupplierId}", id);
        }

        return result;
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        return await _supplierDataService.GetCountriesAsync();
    }

    public async Task<bool> ValidateSupplierCodeAsync(string supplierCode, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
            return false;

        return !await _supplierDataService.SupplierCodeExistsAsync(supplierCode, excludeId);
    }

    private static void ValidateSupplierData(SupplierUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierCode))
            throw new ArgumentException("Supplier code is required.");

        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            throw new ArgumentException("Company name is required.");

        if (dto.SupplierCode.Length > 20)
            throw new ArgumentException("Supplier code cannot exceed 20 characters.");

        if (dto.CompanyName.Length > 255)
            throw new ArgumentException("Company name cannot exceed 255 characters.");

        if (!string.IsNullOrEmpty(dto.Email) && !IsValidEmail(dto.Email))
            throw new ArgumentException("Invalid email format.");

        if (dto.Rating.HasValue && (dto.Rating < 1 || dto.Rating > 5))
            throw new ArgumentException("Rating must be between 1 and 5.");

        if (dto.CreditLimit.HasValue && dto.CreditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative.");
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