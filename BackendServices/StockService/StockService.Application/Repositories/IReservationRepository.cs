using StockService.Application.DTOs;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface IReservationRepository
{
    Task<StockReservation?> GetByIdAsync(Guid reservationId, CancellationToken ct = default);
    Task<StockReservation?> GetByCartIdAsync(Guid cartId, Guid productId, CancellationToken ct = default);
    Task<IEnumerable<StockReservation>> GetByCartIdAsync(Guid cartId, CancellationToken ct = default);
    Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IEnumerable<StockReservation>> GetPendingReservationsAsync(CancellationToken ct = default);
    Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default);
    Task<StockReservation> CreateAsync(StockReservation reservation, CancellationToken ct = default);
    Task UpdateAsync(StockReservation reservation, CancellationToken ct = default);
    Task<int> CommitAsync(Guid reservationId, CancellationToken ct = default);
    Task<int> ReleaseAsync(Guid reservationId, CancellationToken ct = default);
    Task<int> ReleaseByCartIdAsync(Guid cartId, CancellationToken ct = default);
    Task<int> ReleaseExpiredAsync(CancellationToken ct = default);
}
