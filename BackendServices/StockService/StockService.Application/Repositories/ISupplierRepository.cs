using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

/// <summary>
/// Repository interface for Supplier entity operations.
/// </summary>
public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken ct = default);
    Task<Supplier?> GetByCodeAsync(string supplierCode, CancellationToken ct = default);
    Task<IEnumerable<Supplier>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<Supplier>> SearchAsync(string? searchTerm, CancellationToken ct = default);
    Task<Supplier> CreateAsync(Supplier supplier, CancellationToken ct = default);
    Task UpdateAsync(Supplier supplier, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid supplierId, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string supplierCode, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> SetActiveStatusAsync(Guid supplierId, bool isActive, CancellationToken ct = default);
}
