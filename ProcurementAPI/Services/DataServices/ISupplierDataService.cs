using ProcurementAPI.DTOs;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services.DataServices;

public interface ISupplierDataService
{
    Task<PaginatedResult<SupplierDto>> GetSuppliersAsync(
        int page,
        int pageSize,
        string? search,
        string? country,
        int? minRating,
        bool? isActive);

    Task<SupplierDetailDto?> GetSupplierByIdAsync(int id);
    Task<Supplier?> GetSupplierEntityByIdAsync(int id);
    Task<Supplier> CreateSupplierAsync(Supplier supplier);
    Task<Supplier> UpdateSupplierAsync(Supplier supplier);
    Task<bool> DeleteSupplierAsync(int id);
    Task<bool> SupplierExistsAsync(int id);
    Task<bool> SupplierCodeExistsAsync(string supplierCode, int? excludeId = null);
    Task<List<string>> GetCountriesAsync();
    Task<int> GetTotalSuppliersCountAsync();
}