using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly StockServiceDbContext _db;
    private readonly IStockServiceDbContextProcedures _sp;
    private readonly ILogger<ReservationRepository> _logger;

    public ReservationRepository(
        StockServiceDbContext db,
        IStockServiceDbContextProcedures sp,
        ILogger<ReservationRepository> logger)
    {
        _db = db;
        _sp = sp;
        _logger = logger;
    }

    public async Task<StockReservation?> GetByIdAsync(Guid reservationId, CancellationToken ct = default)
    {
        return await _db.StockReservations
            .Include(r => r.StockItem)
            .ThenInclude(s => s.Warehouse)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId, ct);
    }

    public async Task<StockReservation?> GetByCartIdAsync(Guid cartId, Guid productId, CancellationToken ct = default)
    {
        return await _db.StockReservations
            .Include(r => r.StockItem)
            .Where(r => r.CartId == cartId && 
                        r.StockItem.ProductId == productId && 
                        r.ReservationStatus == "Pending")
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<StockReservation>> GetByCartIdAsync(Guid cartId, CancellationToken ct = default)
    {
        return await _db.StockReservations
            .Include(r => r.StockItem)
            .ThenInclude(s => s.Warehouse)
            .Where(r => r.CartId == cartId && r.ReservationStatus == "Pending")
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _db.StockReservations
            .Include(r => r.StockItem)
            .ThenInclude(s => s.Warehouse)
            .Where(r => r.OrderId == orderId)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockReservation>> GetPendingReservationsAsync(CancellationToken ct = default)
    {
        return await _db.StockReservations
            .Include(r => r.StockItem)
            .Where(r => r.ReservationStatus == "Pending")
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.StockReservations
            .Include(r => r.StockItem)
            .Where(r => r.ReservationStatus == "Pending" && 
                        r.ExpiresAt.HasValue && 
                        r.ExpiresAt.Value <= now)
            .ToListAsync(ct);
    }

    public async Task<StockReservation> CreateAsync(StockReservation reservation, CancellationToken ct = default)
    {
        _db.StockReservations.Add(reservation);
        await _db.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task UpdateAsync(StockReservation reservation, CancellationToken ct = default)
    {
        _db.Entry(reservation).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CommitAsync(Guid reservationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Committing reservation {ReservationId}", reservationId);
        return await _sp.SP_CommitStockReservationAsync(reservationId, cancellationToken: ct);
    }

    public async Task<int> ReleaseAsync(Guid reservationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Releasing reservation {ReservationId}", reservationId);
        
        var reservation = await GetByIdAsync(reservationId, ct);
        if (reservation is null || reservation.ReservationStatus != "Pending")
            return 0;

        // Return reserved quantity to available
        var stockItem = await _db.StockItems.FindAsync(new object[] { reservation.StockItemId }, ct);
        if (stockItem is not null)
        {
            stockItem.AvailableQuantity += reservation.ReservedQuantity;
            stockItem.ReservedQuantity -= reservation.ReservedQuantity;
            stockItem.UpdatedAt = DateTime.UtcNow;
        }

        reservation.ReservationStatus = "Released";
        reservation.ReleasedAt = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;

        return await _db.SaveChangesAsync(ct);
    }

    public async Task<int> ReleaseByCartIdAsync(Guid cartId, CancellationToken ct = default)
    {
        _logger.LogInformation("Releasing all reservations for cart {CartId}", cartId);
        
        var reservations = await GetByCartIdAsync(cartId, ct);
        var count = 0;

        foreach (var reservation in reservations)
        {
            var result = await ReleaseAsync(reservation.ReservationId, ct);
            if (result > 0) count++;
        }

        return count;
    }

    public async Task<int> ReleaseExpiredAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Releasing expired reservations");
        return await _sp.SP_ReleaseExpiredReservationsAsync(cancellationToken: ct);
    }
}
