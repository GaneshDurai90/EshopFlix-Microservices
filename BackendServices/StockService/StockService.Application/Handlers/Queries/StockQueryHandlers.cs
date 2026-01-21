using Microsoft.Extensions.Logging;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Queries;
using StockService.Application.Repositories;

namespace StockService.Application.Handlers.Queries;

// ============ Stock Item Query Handlers ============

public class GetStockItemQueryHandler : IQueryHandler<GetStockItemQuery, StockItemDTO?>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<GetStockItemQueryHandler> _logger;

    public GetStockItemQueryHandler(IStockRepository stockRepository, ILogger<GetStockItemQueryHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<StockItemDTO?> HandleAsync(GetStockItemQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting stock item {StockItemId}", query.StockItemId);
        var item = await _stockRepository.GetByIdAsync(query.StockItemId, ct);
        return item is null ? null : MapToDto(item);
    }

    private static StockItemDTO MapToDto(Domain.Entities.StockItem item) => new()
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

public class GetStockByProductQueryHandler : IQueryHandler<GetStockByProductQuery, IEnumerable<StockItemDTO>>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<GetStockByProductQueryHandler> _logger;

    public GetStockByProductQueryHandler(IStockRepository stockRepository, ILogger<GetStockByProductQueryHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockItemDTO>> HandleAsync(GetStockByProductQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting stock for product {ProductId}", query.ProductId);
        var items = await _stockRepository.GetByProductAsync(query.ProductId, query.VariationId, ct);
        return items.Select(item => new StockItemDTO
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
        });
    }
}

public class GetStockSummaryQueryHandler : IQueryHandler<GetStockSummaryQuery, StockItemSummaryDTO?>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<GetStockSummaryQueryHandler> _logger;

    public GetStockSummaryQueryHandler(IStockRepository stockRepository, ILogger<GetStockSummaryQueryHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<StockItemSummaryDTO?> HandleAsync(GetStockSummaryQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting stock summary for product {ProductId}", query.ProductId);
        var items = await _stockRepository.GetByProductAsync(query.ProductId, query.VariationId, ct);
        var itemList = items.ToList();

        if (!itemList.Any()) return null;

        return new StockItemSummaryDTO
        {
            ProductId = query.ProductId,
            Sku = itemList.FirstOrDefault()?.Sku,
            TotalAvailable = itemList.Sum(i => i.AvailableQuantity),
            TotalReserved = itemList.Sum(i => i.ReservedQuantity),
            TotalInTransit = itemList.Sum(i => i.InTransitQuantity),
            WarehouseCount = itemList.Select(i => i.WarehouseId).Distinct().Count(),
            TotalValue = itemList.Sum(i => (i.UnitCost ?? 0) * i.AvailableQuantity),
            IsLowStock = itemList.Any(i => i.MinimumStockLevel.HasValue && i.AvailableQuantity <= i.MinimumStockLevel.Value)
        };
    }
}

public class GetStockByWarehouseQueryHandler : IQueryHandler<GetStockByWarehouseQuery, IEnumerable<StockItemDTO>>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<GetStockByWarehouseQueryHandler> _logger;

    public GetStockByWarehouseQueryHandler(IStockRepository stockRepository, ILogger<GetStockByWarehouseQueryHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockItemDTO>> HandleAsync(GetStockByWarehouseQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting stock for warehouse {WarehouseId}", query.WarehouseId);
        var items = await _stockRepository.GetByWarehouseAsync(query.WarehouseId, ct);
        return items.Select(item => new StockItemDTO
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
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        });
    }
}

// ============ Availability Query Handlers ============

public class GetAvailabilityQueryHandler : IQueryHandler<GetAvailabilityQuery, StockAvailabilityDTO?>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<GetAvailabilityQueryHandler> _logger;

    public GetAvailabilityQueryHandler(IStockRepository stockRepository, ILogger<GetAvailabilityQueryHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<StockAvailabilityDTO?> HandleAsync(GetAvailabilityQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting availability for product {ProductId}", query.ProductId);
        return await _stockRepository.GetAvailabilityAsync(query.ProductId, query.VariationId, ct);
    }
}

public class CheckAvailabilityQueryHandler : IQueryHandler<CheckAvailabilityQuery, CheckAvailabilityResponse>
{
    private readonly IStockRepository _stockRepository;
    private readonly ILogger<CheckAvailabilityQueryHandler> _logger;

    public CheckAvailabilityQueryHandler(IStockRepository stockRepository, ILogger<CheckAvailabilityQueryHandler> logger)
    {
        _stockRepository = stockRepository;
        _logger = logger;
    }

    public async Task<CheckAvailabilityResponse> HandleAsync(CheckAvailabilityQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Checking availability for product {ProductId}, quantity {Quantity}", 
            query.ProductId, query.Quantity);
        
        return await _stockRepository.CheckAvailabilityAsync(new CheckAvailabilityRequest
        {
            ProductId = query.ProductId,
            VariationId = query.VariationId,
            Quantity = query.Quantity,
            PreferredWarehouseId = query.PreferredWarehouseId
        }, ct);
    }
}

// ============ Reservation Query Handlers ============

public class GetCartReservationsQueryHandler : IQueryHandler<GetCartReservationsQuery, IEnumerable<StockReservationDTO>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<GetCartReservationsQueryHandler> _logger;

    public GetCartReservationsQueryHandler(IReservationRepository reservationRepository, ILogger<GetCartReservationsQueryHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockReservationDTO>> HandleAsync(GetCartReservationsQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting reservations for cart {CartId}", query.CartId);
        var reservations = await _reservationRepository.GetByCartIdAsync(query.CartId, ct);
        return reservations.Select(r => new StockReservationDTO
        {
            ReservationId = r.ReservationId,
            StockItemId = r.StockItemId,
            CartId = r.CartId,
            OrderId = r.OrderId,
            CustomerId = r.CustomerId,
            ReservedQuantity = r.ReservedQuantity,
            ReservationStatus = r.ReservationStatus,
            ReservedAt = r.ReservedAt,
            ExpiresAt = r.ExpiresAt,
            ReleasedAt = r.ReleasedAt,
            ReservationType = r.ReservationType,
            UpdatedAt = r.UpdatedAt,
            ProductId = r.StockItem?.ProductId,
            Sku = r.StockItem?.Sku,
            WarehouseName = r.StockItem?.Warehouse?.WarehouseName
        });
    }
}

public class GetOrderReservationsQueryHandler : IQueryHandler<GetOrderReservationsQuery, IEnumerable<StockReservationDTO>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<GetOrderReservationsQueryHandler> _logger;

    public GetOrderReservationsQueryHandler(IReservationRepository reservationRepository, ILogger<GetOrderReservationsQueryHandler> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockReservationDTO>> HandleAsync(GetOrderReservationsQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting reservations for order {OrderId}", query.OrderId);
        var reservations = await _reservationRepository.GetByOrderIdAsync(query.OrderId, ct);
        return reservations.Select(r => new StockReservationDTO
        {
            ReservationId = r.ReservationId,
            StockItemId = r.StockItemId,
            CartId = r.CartId,
            OrderId = r.OrderId,
            CustomerId = r.CustomerId,
            ReservedQuantity = r.ReservedQuantity,
            ReservationStatus = r.ReservationStatus,
            ReservedAt = r.ReservedAt,
            ExpiresAt = r.ExpiresAt,
            ReleasedAt = r.ReleasedAt,
            ReservationType = r.ReservationType,
            UpdatedAt = r.UpdatedAt,
            ProductId = r.StockItem?.ProductId,
            Sku = r.StockItem?.Sku,
            WarehouseName = r.StockItem?.Warehouse?.WarehouseName
        });
    }
}
