using Microsoft.Extensions.Logging;
using StockService.Application.Commands;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Messaging;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Domain.Entities;

namespace StockService.Application.Handlers.Commands;

// ============ Stock Item Command Handlers ============

public class CreateStockItemCommandHandler : ICommandHandler<CreateStockItemCommand, StockItemDTO>
{
    private readonly IStockRepository _stockRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CreateStockItemCommandHandler> _logger;

    public CreateStockItemCommandHandler(
        IStockRepository stockRepository,
        IWarehouseRepository warehouseRepository,
        IIntegrationEventPublisher eventPublisher,
        IIdempotencyService idempotencyService,
        ILogger<CreateStockItemCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _warehouseRepository = warehouseRepository;
        _eventPublisher = eventPublisher;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<StockItemDTO> HandleAsync(CreateStockItemCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling CreateStockItemCommand: Product {ProductId} in Warehouse {WarehouseId}",
            command.ProductId, command.WarehouseId);

        // IDEMPOTENCY CHECK: Use idempotency key if provided, or generate from product+warehouse
        var idempotencyKey = command.IdempotencyKey 
            ?? $"create-stock-{command.ProductId}-{command.WarehouseId}-{command.VariationId}";

        return await _idempotencyService.ExecuteAsync(idempotencyKey, async _ =>
        {
            // Check if stock item already exists for this product/warehouse/variation
            var existing = await _stockRepository.GetByProductAndWarehouseAsync(
                command.ProductId, command.VariationId, command.WarehouseId, ct);
            
            if (existing != null)
            {
                _logger.LogInformation("Stock item already exists: {StockItemId}", existing.StockItemId);
                var warehouse = await _warehouseRepository.GetByIdAsync(command.WarehouseId, ct);
                return MapToDto(existing, warehouse?.WarehouseName);
            }

            // Verify warehouse exists
            var warehouseEntity = await _warehouseRepository.GetByIdAsync(command.WarehouseId, ct)
                ?? throw new InvalidOperationException($"Warehouse {command.WarehouseId} not found");

            var stockItem = new StockItem
            {
                StockItemId = Guid.NewGuid(),
                ProductId = command.ProductId,
                VariationId = command.VariationId,
                WarehouseId = command.WarehouseId,
                Sku = command.Sku,
                AvailableQuantity = command.InitialQuantity,
                ReservedQuantity = 0,
                InTransitQuantity = 0,
                DamagedQuantity = 0,
                MinimumStockLevel = command.MinimumStockLevel,
                MaximumStockLevel = command.MaximumStockLevel,
                ReorderQuantity = command.ReorderQuantity,
                UnitCost = command.UnitCost,
                ExpiryDate = command.ExpiryDate,
                BatchNumber = command.BatchNumber,
                BinLocation = command.BinLocation,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastRestockedAt = command.InitialQuantity > 0 ? DateTime.UtcNow : null
            };

            var created = await _stockRepository.CreateAsync(stockItem, ct);

            // Publish event
            await _eventPublisher.PublishAsync(new StockItemCreatedEvent(
                created.StockItemId,
                command.ProductId,
                command.VariationId,
                command.WarehouseId,
                command.InitialQuantity,
                command.Sku
            ), ct);

            _logger.LogInformation("Created stock item {StockItemId}", created.StockItemId);

            return MapToDto(created, warehouseEntity.WarehouseName);
        }, ct: ct);
    }

    private static StockItemDTO MapToDto(StockItem item, string? warehouseName) => new()
    {
        StockItemId = item.StockItemId,
        ProductId = item.ProductId,
        VariationId = item.VariationId,
        WarehouseId = item.WarehouseId,
        WarehouseName = warehouseName ?? item.Warehouse?.WarehouseName,
        Sku = item.Sku,
        AvailableQuantity = item.AvailableQuantity,
        ReservedQuantity = item.ReservedQuantity,
        InTransitQuantity = item.InTransitQuantity,
        DamagedQuantity = item.DamagedQuantity,
        MinimumStockLevel = item.MinimumStockLevel,
        MaximumStockLevel = item.MaximumStockLevel,
        ReorderQuantity = item.ReorderQuantity,
        UnitCost = item.UnitCost,
        LastRestockedAt = item.LastRestockedAt,
        ExpiryDate = item.ExpiryDate,
        BatchNumber = item.BatchNumber,
        BinLocation = item.BinLocation,
        IsActive = item.IsActive,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };
}

public class UpdateStockItemCommandHandler : ICommandHandler<UpdateStockItemCommand, StockItemDTO>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<UpdateStockItemCommandHandler> _logger;

    public UpdateStockItemCommandHandler(
        IStockRepository stockRepository,
        ILogger<UpdateStockItemCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<StockItemDTO> HandleAsync(UpdateStockItemCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling UpdateStockItemCommand: {StockItemId}", command.StockItemId);

        var item = await _stockRepository.GetByIdAsync(command.StockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {command.StockItemId} not found");

        if (command.MinimumStockLevel.HasValue) item.MinimumStockLevel = command.MinimumStockLevel;
        if (command.MaximumStockLevel.HasValue) item.MaximumStockLevel = command.MaximumStockLevel;
        if (command.ReorderQuantity.HasValue) item.ReorderQuantity = command.ReorderQuantity;
        if (command.UnitCost.HasValue) item.UnitCost = command.UnitCost;
        if (command.BinLocation is not null) item.BinLocation = command.BinLocation;
        if (command.IsActive.HasValue) item.IsActive = command.IsActive.Value;
        item.UpdatedAt = DateTime.UtcNow;

        await _stockRepository.UpdateAsync(item, ct);

        return new StockItemDTO
        {
            StockItemId = item.StockItemId,
            ProductId = item.ProductId,
            VariationId = item.VariationId,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.Warehouse?.WarehouseName,
            Sku = item.Sku,
            AvailableQuantity = item.AvailableQuantity,
            ReservedQuantity = item.ReservedQuantity,
            InTransitQuantity = item.InTransitQuantity,
            DamagedQuantity = item.DamagedQuantity,
            MinimumStockLevel = item.MinimumStockLevel,
            MaximumStockLevel = item.MaximumStockLevel,
            ReorderQuantity = item.ReorderQuantity,
            UnitCost = item.UnitCost,
            LastRestockedAt = item.LastRestockedAt,
            ExpiryDate = item.ExpiryDate,
            BatchNumber = item.BatchNumber,
            BinLocation = item.BinLocation,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}

// ============ Warehouse Command Handlers ============

public class CreateWarehouseCommandHandler : ICommandHandler<CreateWarehouseCommand, WarehouseDTO>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<CreateWarehouseCommandHandler> _logger;

    public CreateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        IIdempotencyService idempotencyService,
        ILogger<CreateWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<WarehouseDTO> HandleAsync(CreateWarehouseCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling CreateWarehouseCommand: {WarehouseCode}", command.WarehouseCode);

        // IDEMPOTENCY CHECK: Use idempotency key if provided, or generate from warehouse code
        var idempotencyKey = command.IdempotencyKey ?? $"create-warehouse-{command.WarehouseCode}";

        return await _idempotencyService.ExecuteAsync(idempotencyKey, async _ =>
        {
            // Check if warehouse already exists with this code
            var existing = await _warehouseRepository.GetByCodeAsync(command.WarehouseCode, ct);
            if (existing != null)
            {
                _logger.LogInformation("Warehouse already exists with code {WarehouseCode}: {WarehouseId}", 
                    command.WarehouseCode, existing.WarehouseId);
                return MapToDto(existing);
            }

            var warehouse = new Warehouse
            {
                WarehouseId = Guid.NewGuid(),
                WarehouseName = command.WarehouseName,
                WarehouseCode = command.WarehouseCode,
                Address = command.Address,
                Type = command.Type,
                IsActive = true,
                Priority = command.Priority,
                Capacity = command.Capacity,
                ContactDetails = command.ContactDetails,
                OperatingHours = command.OperatingHours,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _warehouseRepository.CreateAsync(warehouse, ct);
            _logger.LogInformation("Created warehouse {WarehouseId} with code {WarehouseCode}", 
                created.WarehouseId, created.WarehouseCode);

            return MapToDto(created);
        }, ct: ct);
    }

    private static WarehouseDTO MapToDto(Warehouse w) => new()
    {
        WarehouseId = w.WarehouseId,
        WarehouseName = w.WarehouseName,
        WarehouseCode = w.WarehouseCode,
        Address = w.Address,
        Type = w.Type,
        IsActive = w.IsActive,
        Priority = w.Priority,
        Capacity = w.Capacity,
        ContactDetails = w.ContactDetails,
        OperatingHours = w.OperatingHours,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt
    };
}

public class UpdateWarehouseCommandHandler : ICommandHandler<UpdateWarehouseCommand, WarehouseDTO>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<UpdateWarehouseCommandHandler> _logger;

    public UpdateWarehouseCommandHandler(
        IWarehouseRepository warehouseRepository,
        ILogger<UpdateWarehouseCommandHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    public async Task<WarehouseDTO> HandleAsync(UpdateWarehouseCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling UpdateWarehouseCommand: {WarehouseId}", command.WarehouseId);

        var warehouse = await _warehouseRepository.GetByIdAsync(command.WarehouseId, ct)
            ?? throw new InvalidOperationException($"Warehouse {command.WarehouseId} not found");

        if (command.WarehouseName is not null) warehouse.WarehouseName = command.WarehouseName;
        if (command.Address is not null) warehouse.Address = command.Address;
        if (command.IsActive.HasValue) warehouse.IsActive = command.IsActive.Value;
        if (command.Priority.HasValue) warehouse.Priority = command.Priority.Value;
        if (command.Capacity.HasValue) warehouse.Capacity = command.Capacity;
        if (command.ContactDetails is not null) warehouse.ContactDetails = command.ContactDetails;
        if (command.OperatingHours is not null) warehouse.OperatingHours = command.OperatingHours;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _warehouseRepository.UpdateAsync(warehouse, ct);

        return new WarehouseDTO
        {
            WarehouseId = warehouse.WarehouseId,
            WarehouseName = warehouse.WarehouseName,
            WarehouseCode = warehouse.WarehouseCode,
            Address = warehouse.Address,
            Type = warehouse.Type,
            IsActive = warehouse.IsActive,
            Priority = warehouse.Priority,
            Capacity = warehouse.Capacity,
            ContactDetails = warehouse.ContactDetails,
            OperatingHours = warehouse.OperatingHours,
            CreatedAt = warehouse.CreatedAt,
            UpdatedAt = warehouse.UpdatedAt
        };
    }
}

// ============ Alert Command Handlers ============

public class AcknowledgeAlertCommandHandler : ICommandHandler<AcknowledgeAlertCommand, bool>
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<AcknowledgeAlertCommandHandler> _logger;

    public AcknowledgeAlertCommandHandler(
        IAlertRepository alertRepository,
        ILogger<AcknowledgeAlertCommandHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(AcknowledgeAlertCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling AcknowledgeAlertCommand: {AlertId}", command.AlertId);
        var result = await _alertRepository.AcknowledgeAsync(command.AlertId, command.AcknowledgedBy, ct);
        return result > 0;
    }
}

public class TriggerAlertsCommandHandler : ICommandHandler<TriggerAlertsCommand, int>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<TriggerAlertsCommandHandler> _logger;

    public TriggerAlertsCommandHandler(
        IStockRepository stockRepository,
        ILogger<TriggerAlertsCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<int> HandleAsync(TriggerAlertsCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling TriggerAlertsCommand");
        var lowStock = await _stockRepository.TriggerLowStockAlertsAsync(ct);
        var overStock = await _stockRepository.TriggerOverStockAlertsAsync(ct);
        return lowStock + overStock;
    }
}

// ============ Background Job Command Handlers ============

public class ReleaseExpiredReservationsCommandHandler : ICommandHandler<ReleaseExpiredReservationsCommand, int>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<ReleaseExpiredReservationsCommandHandler> _logger;

    public ReleaseExpiredReservationsCommandHandler(
        IStockRepository stockRepository,
        ILogger<ReleaseExpiredReservationsCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<int> HandleAsync(ReleaseExpiredReservationsCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling ReleaseExpiredReservationsCommand");
        return await _stockRepository.ReleaseExpiredReservationsAsync(ct);
    }
}

public class ExpireStockBatchesCommandHandler : ICommandHandler<ExpireStockBatchesCommand, int>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<ExpireStockBatchesCommandHandler> _logger;

    public ExpireStockBatchesCommandHandler(
        IStockRepository stockRepository,
        ILogger<ExpireStockBatchesCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<int> HandleAsync(ExpireStockBatchesCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling ExpireStockBatchesCommand");
        return await _stockRepository.ExpireStockBatchesAsync(ct);
    }
}

public class RecalculateSafetyStockCommandHandler : ICommandHandler<RecalculateSafetyStockCommand, int>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<RecalculateSafetyStockCommandHandler> _logger;

    public RecalculateSafetyStockCommandHandler(
        IStockRepository stockRepository,
        ILogger<RecalculateSafetyStockCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<int> HandleAsync(RecalculateSafetyStockCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling RecalculateSafetyStockCommand");
        return await _stockRepository.RecalculateSafetyStockAsync(ct);
    }
}
