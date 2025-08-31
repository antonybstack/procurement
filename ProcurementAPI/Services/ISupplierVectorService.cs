using Microsoft.Extensions.VectorData;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services;

public interface ISupplierVectorService
{
    Task<IList<SupplierVector>> VectorizeSuppliersAsync(int? count);
    Task InitializeVectorStoreAsync();
    Task<IList<SupplierVector>> CreateTestDataAsync();
    Task<IAsyncEnumerable<VectorSearchResult<SupplierVector>>> SearchByHybridAsync(string searchText, int top = 1);
    Task<IAsyncEnumerable<VectorSearchResult<SupplierVector>>> SearchByVectorAsync(string searchText, int top = 1);
}