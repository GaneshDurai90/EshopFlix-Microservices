using Microsoft.Extensions.Logging;
using StockService.Application.Commands;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Messaging;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Domain.Entities;

namespace StockService.Application.Handlers.Commands;

// ============ Stock Adjustment Command Handlers ============

public class IncreaseStockCommandHandler : ICommandHandler<IncreaseStockCommand, StockAdjustmentDTO>
{
    private readonly IStockRepository _stockRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IncreaseStockCommandHandler> _logger;

    public IncreaseStockCommandHandler(
        IStockRepository stockRepository,
        IIntegrationEventPublisher eventPublisher,
        IIdempotencyService idempotencyService,
        ILogger<IncreaseStockCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _eventPublisher = eventPublisher;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<StockAdjustmentDTO> HandleAsync(IncreaseStockCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling IncreaseStockCommand: {StockItemId} by {Quantity}",
            command.StockItemId, command.Quantity);

        // IDEMPOTENCY CHECK: Critical for stock adjustments to prevent duplicate increases
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return await _idempotencyService.ExecuteAsync(command.IdempotencyKey, async _ =>
            {
                return await ExecuteIncrease(command, ct);
            }, ct: ct);
        }

        return await ExecuteIncrease(command, ct);
    }

    private async Task<StockAdjustmentDTO> ExecuteIncrease(IncreaseStockCommand command, CancellationToken ct)
    {
        var item = await _stockRepository.GetByIdAsync(command.StockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {command.StockItemId} not found");

        var quantityBefore = item.AvailableQuantity;
        await _stockRepository.IncreaseStockAsync(command.StockItemId, command.Quantity, command.Reason, command.PerformedBy, ct);

        // Publish event
        await _eventPublisher.PublishAsync(new StockAdjustedEvent(
            command.StockItemId,
            item.ProductId,
            "Increase",
            command.Quantity,
            quantityBefore,
            quantityBefore + command.Quantity,
            command.Reason,
            command.PerformedBy
        ), ct);

        return new StockAdjustmentDTO
        {
            AdjustmentId = Guid.NewGuid(),
            StockItemId = command.StockItemId,
            ProductId = item.ProductId,
            Sku = item.Sku,
            AdjustmentType = "Increase",
            AdjustmentQuantity = command.Quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityBefore + command.Quantity,
            Reason = command.Reason,
            PerformedBy = command.PerformedBy,
            AdjustmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class DecreaseStockCommandHandler : ICommandHandler<DecreaseStockCommand, StockAdjustmentDTO>
{
    private readonly IStockRepository _stockRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<DecreaseStockCommandHandler> _logger;

    public DecreaseStockCommandHandler(
        IStockRepository stockRepository,
        IIntegrationEventPublisher eventPublisher,
        IIdempotencyService idempotencyService,
        ILogger<DecreaseStockCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _eventPublisher = eventPublisher;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<StockAdjustmentDTO> HandleAsync(DecreaseStockCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling DecreaseStockCommand: {StockItemId} by {Quantity}",
            command.StockItemId, command.Quantity);

        // IDEMPOTENCY CHECK: Critical for stock adjustments to prevent duplicate decreases
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return await _idempotencyService.ExecuteAsync(command.IdempotencyKey, async _ =>
            {
                return await ExecuteDecrease(command, ct);
            }, ct: ct);
        }

        return await ExecuteDecrease(command, ct);
    }

    private async Task<StockAdjustmentDTO> ExecuteDecrease(DecreaseStockCommand command, CancellationToken ct)
    {
        var item = await _stockRepository.GetByIdAsync(command.StockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {command.StockItemId} not found");

        var quantityBefore = item.AvailableQuantity;
        await _stockRepository.DecreaseStockAsync(command.StockItemId, command.Quantity, command.Reason, command.PerformedBy, ct);

        // Publish event
        await _eventPublisher.PublishAsync(new StockAdjustedEvent(
            command.StockItemId,
            item.ProductId,
            "Decrease",
            -command.Quantity,
            quantityBefore,
            quantityBefore - command.Quantity,
            command.Reason,
            command.PerformedBy
        ), ct);

        // Check for low stock alert
        var newQuantity = quantityBefore - command.Quantity;
        if (item.MinimumStockLevel.HasValue && newQuantity <= item.MinimumStockLevel.Value)
        {
            await _eventPublisher.PublishAsync(new LowStockAlertEvent(
                command.StockItemId,
                item.ProductId,
                item.Sku,
                item.WarehouseId,
                newQuantity,
                item.MinimumStockLevel.Value
            ), ct);
        }

        return new StockAdjustmentDTO
        {
            AdjustmentId = Guid.NewGuid(),
            StockItemId = command.StockItemId,
            ProductId = item.ProductId,
            Sku = item.Sku,
            AdjustmentType = "Decrease",
            AdjustmentQuantity = -command.Quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = newQuantity,
            Reason = command.Reason,
            PerformedBy = command.PerformedBy,
            AdjustmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class AdjustStockCommandHandler : ICommandHandler<AdjustStockCommand, StockAdjustmentDTO>
{
    private readonly IStockRepository _stockRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<AdjustStockCommandHandler> _logger;

    public AdjustStockCommandHandler(
        IStockRepository stockRepository,
        IIntegrationEventPublisher eventPublisher,
        IIdempotencyService idempotencyService,
        ILogger<AdjustStockCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _eventPublisher = eventPublisher;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<StockAdjustmentDTO> HandleAsync(AdjustStockCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling AdjustStockCommand: {StockItemId}, Type {Type}, Qty {Quantity}",
            command.StockItemId, command.AdjustmentType, command.AdjustmentQuantity);

        // IDEMPOTENCY CHECK: Critical for stock adjustments
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return await _idempotencyService.ExecuteAsync(command.IdempotencyKey, async _ =>
            {
                return await ExecuteAdjust(command, ct);
            }, ct: ct);
        }

        return await ExecuteAdjust(command, ct);
    }

    private async Task<StockAdjustmentDTO> ExecuteAdjust(AdjustStockCommand command, CancellationToken ct)
    {
        var item = await _stockRepository.GetByIdAsync(command.StockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {command.StockItemId} not found");

        var quantityBefore = item.AvailableQuantity;
        await _stockRepository.AdjustStockAsync(
            command.StockItemId,
            command.AdjustmentQuantity,
            command.Reason ?? command.AdjustmentType,
            command.PerformedBy,
            command.ApprovedBy,
            ct);

        var quantityAfter = quantityBefore + command.AdjustmentQuantity;

        // Publish event
        await _eventPublisher.PublishAsync(new StockAdjustedEvent(
            command.StockItemId,
            item.ProductId,
            command.AdjustmentType,
            command.AdjustmentQuantity,
            quantityBefore,
            quantityAfter,
            command.Reason,
            command.PerformedBy
        ), ct);

        return new StockAdjustmentDTO
        {
            AdjustmentId = Guid.NewGuid(),
            StockItemId = command.StockItemId,
            ProductId = item.ProductId,
            Sku = item.Sku,
            AdjustmentType = command.AdjustmentType,
            AdjustmentQuantity = command.AdjustmentQuantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            Reason = command.Reason,
            Notes = command.Notes,
            PerformedBy = command.PerformedBy,
            ApprovedBy = command.ApprovedBy,
            AdjustmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
