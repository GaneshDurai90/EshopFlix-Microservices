using Microsoft.Extensions.Logging;
using StockService.Application.Commands;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Messaging;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Domain.Entities;

namespace StockService.Application.Handlers.Commands;

// ============ Reservation Command Handlers ============

public class ReserveStockCommandHandler : ICommandHandler<ReserveStockCommand, CreateReservationResponse>
{
    private readonly IStockRepository _stockRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<ReserveStockCommandHandler> _logger;

    public ReserveStockCommandHandler(
        IStockRepository stockRepository,
        IReservationRepository reservationRepository,
        IIntegrationEventPublisher eventPublisher,
        IIdempotencyService idempotencyService,
        ILogger<ReserveStockCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _reservationRepository = reservationRepository;
        _eventPublisher = eventPublisher;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<CreateReservationResponse> HandleAsync(ReserveStockCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling ReserveStockCommand: Product {ProductId}, Qty {Quantity}, Cart {CartId}",
            command.ProductId, command.Quantity, command.CartId);

        // IDEMPOTENCY: Use explicit key if provided
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return await _idempotencyService.ExecuteAsync(command.IdempotencyKey, async _ =>
            {
                return await ExecuteReservation(command, ct);
            }, ct: ct);
        }

        return await ExecuteReservation(command, ct);
    }

    private async Task<CreateReservationResponse> ExecuteReservation(ReserveStockCommand command, CancellationToken ct)
    {
        try
        {
            // BUSINESS IDEMPOTENCY: If CartId is provided, check for existing reservation
            if (command.CartId.HasValue)
            {
                var existingReservation = await _reservationRepository.GetByCartIdAsync(
                    command.CartId.Value, command.ProductId, ct);
                
                if (existingReservation != null)
                {
                    _logger.LogInformation(
                        "Found existing reservation {ReservationId} for Cart {CartId}, Product {ProductId}",
                        existingReservation.ReservationId, command.CartId, command.ProductId);

                    // Return existing reservation info (idempotent response)
                    return new CreateReservationResponse
                    {
                        ReservationId = existingReservation.ReservationId,
                        StockItemId = existingReservation.StockItemId,
                        ReservedQuantity = existingReservation.ReservedQuantity,
                        ExpiresAt = existingReservation.ExpiresAt ?? DateTime.UtcNow.AddMinutes(15),
                        WarehouseName = existingReservation.StockItem?.Warehouse?.WarehouseName ?? "Unknown",
                        Success = true,
                        Message = "Existing reservation found"
                    };
                }
            }

            // Check availability
            var availability = await _stockRepository.CheckAvailabilityAsync(new CheckAvailabilityRequest
            {
                ProductId = command.ProductId,
                VariationId = command.VariationId,
                Quantity = command.Quantity,
                PreferredWarehouseId = command.WarehouseId
            }, ct);

            if (!availability.IsAvailable || !availability.Allocations.Any())
            {
                _logger.LogWarning("Insufficient stock for product {ProductId}", command.ProductId);
                return new CreateReservationResponse
                {
                    Success = false,
                    Message = $"Insufficient stock. Available: {availability.AvailableQuantity}, Requested: {command.Quantity}"
                };
            }

            // Use first allocation
            var allocation = availability.Allocations.First();
            var reservationId = Guid.NewGuid();
            var ttl = command.TtlMinutes ?? 15;
            var expiresAt = DateTime.UtcNow.AddMinutes(ttl);

            var reservation = new StockReservation
            {
                ReservationId = reservationId,
                StockItemId = allocation.StockItemId,
                CartId = command.CartId,
                OrderId = command.OrderId,
                CustomerId = command.CustomerId,
                ReservedQuantity = allocation.AllocatedQuantity,
                ReservationStatus = "Pending",
                ReservationType = command.ReservationType,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                UpdatedAt = DateTime.UtcNow
            };

            // Create reservation in database
            await _reservationRepository.CreateAsync(reservation, ct);
            
            // Update stock quantities (decrease available, increase reserved)
            await _stockRepository.ReserveStockAsync(allocation.StockItemId, reservationId, allocation.AllocatedQuantity, ct);

            // Publish integration event
            await _eventPublisher.PublishAsync(new StockReservedEvent(
                reservationId,
                command.ProductId,
                command.VariationId,
                allocation.StockItemId,
                allocation.AllocatedQuantity,
                command.CartId,
                command.OrderId,
                expiresAt
            ), ct);

            _logger.LogInformation("Stock reserved: ReservationId {ReservationId}, Quantity {Quantity}",
                reservationId, allocation.AllocatedQuantity);

            return new CreateReservationResponse
            {
                ReservationId = reservationId,
                StockItemId = allocation.StockItemId,
                ReservedQuantity = allocation.AllocatedQuantity,
                ExpiresAt = expiresAt,
                WarehouseName = allocation.WarehouseName,
                Success = true,
                Message = "Stock reserved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for Product {ProductId}, Cart {CartId}",
                command.ProductId, command.CartId);
            
            return new CreateReservationResponse
            {
                Success = false,
                Message = $"Error reserving stock: {ex.Message}"
            };
        }
    }
}

public class CommitReservationCommandHandler : ICommandHandler<CommitReservationCommand, bool>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CommitReservationCommandHandler> _logger;

    public CommitReservationCommandHandler(
        IReservationRepository reservationRepository,
        IStockRepository stockRepository,
        IIntegrationEventPublisher eventPublisher,
        IIdempotencyService idempotencyService,
        ILogger<CommitReservationCommandHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _stockRepository = stockRepository;
        _eventPublisher = eventPublisher;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(CommitReservationCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling CommitReservationCommand: {ReservationId} for Order {OrderId}",
            command.ReservationId, command.OrderId);

        // IDEMPOTENCY: Use explicit key if provided, or generate from reservation+order
        var idempotencyKey = command.IdempotencyKey 
            ?? $"commit-reservation-{command.ReservationId}-{command.OrderId}";

        return await _idempotencyService.ExecuteAsync(idempotencyKey, async _ =>
        {
            var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, ct);
            if (reservation is null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found", command.ReservationId);
                return false;
            }

            // IDEMPOTENCY: If already committed, return success
            if (reservation.ReservationStatus == "Committed" && reservation.OrderId == command.OrderId)
            {
                _logger.LogInformation("Reservation {ReservationId} already committed for Order {OrderId}",
                    command.ReservationId, command.OrderId);
                return true;
            }

            reservation.OrderId = command.OrderId;
            reservation.ReservationStatus = "Committed";
            reservation.ReservationType = "Order";
            reservation.ExpiresAt = null; // Committed reservations don't expire
            reservation.UpdatedAt = DateTime.UtcNow;

            await _reservationRepository.UpdateAsync(reservation, ct);
            await _stockRepository.CommitReservationAsync(command.ReservationId, ct);

            // Publish integration event
            await _eventPublisher.PublishAsync(new StockCommittedEvent(
                command.ReservationId,
                command.OrderId,
                reservation.StockItemId,
                reservation.ReservedQuantity
            ), ct);

            _logger.LogInformation("Reservation {ReservationId} committed for Order {OrderId}",
                command.ReservationId, command.OrderId);

            return true;
        }, ct: ct);
    }
}

public class ReleaseReservationCommandHandler : ICommandHandler<ReleaseReservationCommand, bool>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<ReleaseReservationCommandHandler> _logger;

    public ReleaseReservationCommandHandler(
        IReservationRepository reservationRepository,
        IIntegrationEventPublisher eventPublisher,
        ILogger<ReleaseReservationCommandHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ReleaseReservationCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling ReleaseReservationCommand: {ReservationId}", command.ReservationId);

        var reservation = await _reservationRepository.GetByIdAsync(command.ReservationId, ct);
        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", command.ReservationId);
            return false;
        }

        var result = await _reservationRepository.ReleaseAsync(command.ReservationId, ct);
        if (result > 0)
        {
            // Publish integration event
            await _eventPublisher.PublishAsync(new StockReleasedEvent(
                command.ReservationId,
                reservation.StockItemId,
                reservation.ReservedQuantity,
                command.Reason ?? "Released by request"
            ), ct);

            _logger.LogInformation("Reservation {ReservationId} released", command.ReservationId);
        }

        return result > 0;
    }
}

public class ReleaseCartReservationsCommandHandler : ICommandHandler<ReleaseCartReservationsCommand, int>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<ReleaseCartReservationsCommandHandler> _logger;

    public ReleaseCartReservationsCommandHandler(
        IReservationRepository reservationRepository,
        IIntegrationEventPublisher eventPublisher,
        ILogger<ReleaseCartReservationsCommandHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<int> HandleAsync(ReleaseCartReservationsCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling ReleaseCartReservationsCommand: Cart {CartId}", command.CartId);

        var reservations = await _reservationRepository.GetByCartIdAsync(command.CartId, ct);
        var count = 0;

        foreach (var reservation in reservations)
        {
            var result = await _reservationRepository.ReleaseAsync(reservation.ReservationId, ct);
            if (result > 0)
            {
                await _eventPublisher.PublishAsync(new StockReleasedEvent(
                    reservation.ReservationId,
                    reservation.StockItemId,
                    reservation.ReservedQuantity,
                    "Cart cleared"
                ), ct);
                count++;
            }
        }

        _logger.LogInformation("Released {Count} reservations for cart {CartId}", count, command.CartId);
        return count;
    }
}
