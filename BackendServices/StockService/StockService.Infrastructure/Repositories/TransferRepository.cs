using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

/// <summary>
/// Repository for stock transfer operations.
/// </summary>
public class TransferRepository : ITransferRepository
{
    private readonly StockServiceDbContext _db;
    private readonly ILogger<TransferRepository> _logger;

    public TransferRepository(StockServiceDbContext db, ILogger<TransferRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<StockTransfer?> GetByIdAsync(Guid transferId, CancellationToken ct = default)
    {
        return await _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .FirstOrDefaultAsync(t => t.TransferId == transferId, ct);
    }

    public async Task<StockTransfer?> GetByIdWithItemsAsync(Guid transferId, CancellationToken ct = default)
    {
        return await _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.StockTransferItems)
                .ThenInclude(i => i.StockItem)
            .FirstOrDefaultAsync(t => t.TransferId == transferId, ct);
    }

    public async Task<IEnumerable<StockTransfer>> GetByWarehouseAsync(Guid warehouseId, bool isSource, CancellationToken ct = default)
    {
        var query = _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse);

        if (isSource)
        {
            return await query.Where(t => t.FromWarehouseId == warehouseId).ToListAsync(ct);
        }
        else
        {
            return await query.Where(t => t.ToWarehouseId == warehouseId).ToListAsync(ct);
        }
    }

    public async Task<IEnumerable<StockTransfer>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        return await _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Where(t => t.TransferStatus == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockTransfer>> GetInTransitAsync(CancellationToken ct = default)
    {
        return await _db.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Where(t => t.TransferStatus == "InTransit")
            .OrderBy(t => t.EstimatedArrival)
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateAsync(Guid fromWarehouseId, Guid toWarehouseId, Guid requestedBy, string? notes, CancellationToken ct = default)
    {
        var transfer = new StockTransfer
        {
            TransferId = Guid.NewGuid(),
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            TransferStatus = "Pending",
            RequestedBy = requestedBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.StockTransfers.Add(transfer);
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created stock transfer {TransferId} from {From} to {To}", 
            transfer.TransferId, fromWarehouseId, toWarehouseId);
        
        return transfer.TransferId;
    }

    public async Task AddTransferItemAsync(StockTransferItem item, CancellationToken ct = default)
    {
        _db.StockTransferItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(StockTransfer transfer, CancellationToken ct = default)
    {
        transfer.UpdatedAt = DateTime.UtcNow;
        _db.Entry(transfer).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CompleteTransferAsync(Guid transferId, CancellationToken ct = default)
    {
        var transfer = await GetByIdAsync(transferId, ct);
        if (transfer == null) return 0;

        transfer.TransferStatus = "Completed";
        transfer.ReceivedAt = DateTime.UtcNow;
        transfer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("Completed stock transfer {TransferId}", transferId);
        return 1;
    }
}
