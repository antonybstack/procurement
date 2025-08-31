using Microsoft.Extensions.VectorData;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services;

public interface ISupplierVectorService
{
    Task<IList<SupplierVector>> VectorizeSuppliersAsync(int? count);
    Task InitializeVectorStoreAsync();
    Task<IList<SupplierVector>> CreateTestDataAsync();

    // Task<IAsyncEnumerable<VectorSearchResult<SupplierVector>>> SearchByHybridAsync(
    //     string searchValue,
    //     int top,
    //     CancellationToken cancellationToken);

    Task<IAsyncEnumerable<VectorSearchResult<SupplierVector>>> SearchByVectorAsync(
        string searchValue,
        int top,
        CancellationToken cancellationToken);
}