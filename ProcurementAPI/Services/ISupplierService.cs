using ProcurementAPI.DTOs;

namespace ProcurementAPI.Services;

public interface ISupplierService
{
    Task<PaginatedResult<SupplierDto>> GetSuppliersAsync(
        int page,
        int pageSize,
        string? search,
        string? country,
        int? minRating,
        bool? isActive);

    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<SupplierDto> CreateSupplierAsync(SupplierUpdateDto createDto);
    Task<SupplierDto> UpdateSupplierAsync(int id, SupplierUpdateDto updateDto);
    Task<bool> DeleteSupplierAsync(int id);
    Task<List<string>> GetCountriesAsync();
    Task<bool> ValidateSupplierCodeAsync(string supplierCode, int? excludeId = null);
}