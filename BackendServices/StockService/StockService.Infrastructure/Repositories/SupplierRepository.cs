using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

/// <summary>
/// Repository for Supplier entity operations.
/// </summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly StockServiceDbContext _db;
    private readonly ILogger<SupplierRepository> _logger;

    public SupplierRepository(StockServiceDbContext db, ILogger<SupplierRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Supplier?> GetByIdAsync(Guid supplierId, CancellationToken ct = default)
    {
        return await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId, ct);
    }

    public async Task<Supplier?> GetByCodeAsync(string supplierCode, CancellationToken ct = default)
    {
        return await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierCode == supplierCode, ct);
    }

    public async Task<IEnumerable<Supplier>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _db.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.SupplierName)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Supplier>> SearchAsync(string? searchTerm, CancellationToken ct = default)
    {
        var query = _db.Suppliers.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => 
                s.SupplierName.Contains(searchTerm) || 
                s.SupplierCode.Contains(searchTerm) ||
                (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)));
        }

        return await query.OrderBy(s => s.SupplierName).ToListAsync(ct);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating supplier {SupplierCode}", supplier.SupplierCode);
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(ct);
        return supplier;
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating supplier {SupplierId}", supplier.SupplierId);
        _db.Entry(supplier).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid supplierId, CancellationToken ct = default)
    {
        return await _db.Suppliers.AnyAsync(s => s.SupplierId == supplierId, ct);
    }

    public async Task<bool> CodeExistsAsync(string supplierCode, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _db.Suppliers.Where(s => s.SupplierCode == supplierCode);
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.SupplierId != excludeId.Value);
        }
        return await query.AnyAsync(ct);
    }

    public async Task<bool> SetActiveStatusAsync(Guid supplierId, bool isActive, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId, ct);
        if (supplier != null)
        {
            supplier.IsActive = isActive;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }
        return false;
    }
}
