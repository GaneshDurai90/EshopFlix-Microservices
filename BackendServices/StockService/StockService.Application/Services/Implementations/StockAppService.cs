using Microsoft.Extensions.Logging;
using StockService.Application.DTOs;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Domain.Entities;

namespace StockService.Application.Services.Implementations;

public class StockAppService : IStockAppService
{
    private readonly IStockRepository _stockRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<StockAppService> _logger;

    public StockAppService(
        IStockRepository stockRepository,
        IReservationRepository reservationRepository,
        IWarehouseRepository warehouseRepository,
        ITransferRepository transferRepository,
        IAlertRepository alertRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ISupplierRepository supplierRepository,
        IReportRepository reportRepository,
        ILogger<StockAppService> logger)
    {
        _stockRepository = stockRepository;
        _reservationRepository = reservationRepository;
        _warehouseRepository = warehouseRepository;
        _transferRepository = transferRepository;
        _alertRepository = alertRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _supplierRepository = supplierRepository;
        _reportRepository = reportRepository;
        _logger = logger;
    }

    // ============ Stock Item Operations ============

    public async Task<StockItemDTO?> GetStockItemAsync(Guid stockItemId, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting stock item {StockItemId}", stockItemId);
        var item = await _stockRepository.GetByIdAsync(stockItemId, ct);
        return item is null ? null : MapToDto(item);
    }

    public async Task<IEnumerable<StockItemDTO>> GetStockByProductAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting stock for product {ProductId}, variation {VariationId}", productId, variationId);
        var items = await _stockRepository.GetByProductAsync(productId, variationId, ct);
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<StockItemDTO>> GetStockByWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting stock for warehouse {WarehouseId}", warehouseId);
        var items = await _stockRepository.GetByWarehouseAsync(warehouseId, ct);
        return items.Select(MapToDto);
    }

    public async Task<StockItemSummaryDTO?> GetStockSummaryByProductAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting stock summary for product {ProductId}", productId);
        var items = await _stockRepository.GetByProductAsync(productId, variationId, ct);
        var itemList = items.ToList();
        
        if (!itemList.Any()) return null;

        return new StockItemSummaryDTO
        {
            ProductId = productId,
            Sku = itemList.FirstOrDefault()?.Sku,
            TotalAvailable = itemList.Sum(i => i.AvailableQuantity),
            TotalReserved = itemList.Sum(i => i.ReservedQuantity),
            TotalInTransit = itemList.Sum(i => i.InTransitQuantity),
            WarehouseCount = itemList.Select(i => i.WarehouseId).Distinct().Count(),
            TotalValue = itemList.Sum(i => (i.UnitCost ?? 0) * i.AvailableQuantity),
            IsLowStock = itemList.Any(i => i.MinimumStockLevel.HasValue && i.AvailableQuantity <= i.MinimumStockLevel.Value)
        };
    }

    public async Task<StockItemDTO> CreateStockItemAsync(CreateStockItemRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating stock item for product {ProductId} in warehouse {WarehouseId}", 
            request.ProductId, request.WarehouseId);

        var stockItem = new StockItem
        {
            StockItemId = Guid.NewGuid(),
            ProductId = request.ProductId,
            VariationId = request.VariationId,
            WarehouseId = request.WarehouseId,
            Sku = request.Sku,
            AvailableQuantity = request.InitialQuantity,
            ReservedQuantity = 0,
            InTransitQuantity = 0,
            DamagedQuantity = 0,
            MinimumStockLevel = request.MinimumStockLevel,
            MaximumStockLevel = request.MaximumStockLevel,
            ReorderQuantity = request.ReorderQuantity,
            UnitCost = request.UnitCost,
            ExpiryDate = request.ExpiryDate,
            BatchNumber = request.BatchNumber,
            BinLocation = request.BinLocation,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastRestockedAt = request.InitialQuantity > 0 ? DateTime.UtcNow : null
        };

        var created = await _stockRepository.CreateAsync(stockItem, ct);
        _logger.LogInformation("Created stock item {StockItemId}", created.StockItemId);
        
        return MapToDto(created);
    }

    public async Task<StockItemDTO> UpdateStockItemAsync(UpdateStockItemRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating stock item {StockItemId}", request.StockItemId);
        
        var item = await _stockRepository.GetByIdAsync(request.StockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {request.StockItemId} not found");

        if (request.MinimumStockLevel.HasValue) item.MinimumStockLevel = request.MinimumStockLevel;
        if (request.MaximumStockLevel.HasValue) item.MaximumStockLevel = request.MaximumStockLevel;
        if (request.ReorderQuantity.HasValue) item.ReorderQuantity = request.ReorderQuantity;
        if (request.UnitCost.HasValue) item.UnitCost = request.UnitCost;
        if (request.BinLocation is not null) item.BinLocation = request.BinLocation;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
        item.UpdatedAt = DateTime.UtcNow;

        await _stockRepository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    // ============ Availability & Allocation ============

    public async Task<StockAvailabilityDTO?> GetAvailabilityAsync(Guid productId, Guid? variationId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking availability for product {ProductId}", productId);
        return await _stockRepository.GetAvailabilityAsync(productId, variationId, ct);
    }

    public async Task<CheckAvailabilityResponse> CheckAvailabilityAsync(CheckAvailabilityRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Checking availability: Product {ProductId}, Quantity {Quantity}", 
            request.ProductId, request.Quantity);
        return await _stockRepository.CheckAvailabilityAsync(request, ct);
    }

    public async Task<IEnumerable<AllocationSuggestionDTO>> GetAllocationSuggestionsAsync(
        Guid productId, Guid? variationId, int quantity, CancellationToken ct = default)
    {
        var response = await CheckAvailabilityAsync(new CheckAvailabilityRequest
        {
            ProductId = productId,
            VariationId = variationId,
            Quantity = quantity
        }, ct);

        return response.Allocations;
    }

    // ============ Reservation Operations ============

    public async Task<CreateReservationResponse> ReserveStockAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Reserving stock: Product {ProductId}, Quantity {Quantity}, Cart {CartId}", 
            request.ProductId, request.Quantity, request.CartId);

        // Find available stock
        var availability = await CheckAvailabilityAsync(new CheckAvailabilityRequest
        {
            ProductId = request.ProductId,
            VariationId = request.VariationId,
            Quantity = request.Quantity,
            PreferredWarehouseId = request.WarehouseId
        }, ct);

        if (!availability.IsAvailable || !availability.Allocations.Any())
        {
            _logger.LogWarning("Insufficient stock for product {ProductId}", request.ProductId);
            return new CreateReservationResponse
            {
                Success = false,
                Message = $"Insufficient stock. Available: {availability.AvailableQuantity}, Requested: {request.Quantity}"
            };
        }

        // Use first allocation suggestion
        var allocation = availability.Allocations.First();
        var reservationId = Guid.NewGuid();
        var ttl = request.TtlMinutes ?? 15; // Default 15 minutes for cart reservations
        var expiresAt = DateTime.UtcNow.AddMinutes(ttl);

        var reservation = new StockReservation
        {
            ReservationId = reservationId,
            StockItemId = allocation.StockItemId,
            CartId = request.CartId,
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            ReservedQuantity = allocation.AllocatedQuantity,
            ReservationStatus = "Pending",
            ReservationType = request.ReservationType,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            UpdatedAt = DateTime.UtcNow
        };

        await _reservationRepository.CreateAsync(reservation, ct);
        
        // Reserve the stock via SP
        await _stockRepository.ReserveStockAsync(allocation.StockItemId, reservationId, allocation.AllocatedQuantity, ct);

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

    public async Task<bool> CommitReservationAsync(CommitReservationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Committing reservation {ReservationId} for order {OrderId}", 
            request.ReservationId, request.OrderId);

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, ct);
        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", request.ReservationId);
            return false;
        }

        reservation.OrderId = request.OrderId;
        reservation.ReservationStatus = "Committed";
        reservation.ReservationType = "Order";
        reservation.ExpiresAt = null; // Committed reservations don't expire
        reservation.UpdatedAt = DateTime.UtcNow;

        await _reservationRepository.UpdateAsync(reservation, ct);
        await _stockRepository.CommitReservationAsync(request.ReservationId, ct);

        _logger.LogInformation("Reservation {ReservationId} committed", request.ReservationId);
        return true;
    }

    public async Task<bool> ReleaseReservationAsync(ReleaseReservationRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Releasing reservation {ReservationId}", request.ReservationId);

        var result = await _reservationRepository.ReleaseAsync(request.ReservationId, ct);
        return result > 0;
    }

    public async Task<bool> ReleaseReservationsByCartAsync(Guid cartId, CancellationToken ct = default)
    {
        _logger.LogInformation("Releasing all reservations for cart {CartId}", cartId);

        var result = await _reservationRepository.ReleaseByCartIdAsync(cartId, ct);
        return result > 0;
    }

    public async Task<IEnumerable<StockReservationDTO>> GetReservationsByCartAsync(Guid cartId, CancellationToken ct = default)
    {
        var reservations = await _reservationRepository.GetByCartIdAsync(cartId, ct);
        return reservations.Select(MapReservationToDto);
    }

    public async Task<IEnumerable<StockReservationDTO>> GetReservationsByOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var reservations = await _reservationRepository.GetByOrderIdAsync(orderId, ct);
        return reservations.Select(MapReservationToDto);
    }

    // ============ Stock Adjustments ============

    public async Task<StockAdjustmentDTO> IncreaseStockAsync(
        Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Increasing stock {StockItemId} by {Quantity}", stockItemId, quantity);
        
        var item = await _stockRepository.GetByIdAsync(stockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {stockItemId} not found");

        var quantityBefore = item.AvailableQuantity;
        await _stockRepository.IncreaseStockAsync(stockItemId, quantity, reason, performedBy, ct);

        return new StockAdjustmentDTO
        {
            AdjustmentId = Guid.NewGuid(),
            StockItemId = stockItemId,
            ProductId = item.ProductId,
            Sku = item.Sku,
            AdjustmentType = "Increase",
            AdjustmentQuantity = quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityBefore + quantity,
            Reason = reason,
            PerformedBy = performedBy,
            AdjustmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<StockAdjustmentDTO> DecreaseStockAsync(
        Guid stockItemId, int quantity, string reason, Guid performedBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Decreasing stock {StockItemId} by {Quantity}", stockItemId, quantity);
        
        var item = await _stockRepository.GetByIdAsync(stockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {stockItemId} not found");

        var quantityBefore = item.AvailableQuantity;
        await _stockRepository.DecreaseStockAsync(stockItemId, quantity, reason, performedBy, ct);

        return new StockAdjustmentDTO
        {
            AdjustmentId = Guid.NewGuid(),
            StockItemId = stockItemId,
            ProductId = item.ProductId,
            Sku = item.Sku,
            AdjustmentType = "Decrease",
            AdjustmentQuantity = -quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityBefore - quantity,
            Reason = reason,
            PerformedBy = performedBy,
            AdjustmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<StockAdjustmentDTO> AdjustStockAsync(CreateAdjustmentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Adjusting stock {StockItemId}: {Type} {Quantity}", 
            request.StockItemId, request.AdjustmentType, request.AdjustmentQuantity);

        var item = await _stockRepository.GetByIdAsync(request.StockItemId, ct)
            ?? throw new InvalidOperationException($"Stock item {request.StockItemId} not found");

        var quantityBefore = item.AvailableQuantity;
        await _stockRepository.AdjustStockAsync(
            request.StockItemId, 
            request.AdjustmentQuantity, 
            request.Reason ?? request.AdjustmentType, 
            request.PerformedBy, 
            request.ApprovedBy, 
            ct);

        return new StockAdjustmentDTO
        {
            AdjustmentId = Guid.NewGuid(),
            StockItemId = request.StockItemId,
            ProductId = item.ProductId,
            Sku = item.Sku,
            AdjustmentType = request.AdjustmentType,
            AdjustmentQuantity = request.AdjustmentQuantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityBefore + request.AdjustmentQuantity,
            Reason = request.Reason,
            Notes = request.Notes,
            PerformedBy = request.PerformedBy,
            ApprovedBy = request.ApprovedBy,
            AdjustmentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============ Warehouse Operations ============

    public async Task<WarehouseDTO?> GetWarehouseAsync(Guid warehouseId, CancellationToken ct = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, ct);
        return warehouse is null ? null : MapWarehouseToDto(warehouse);
    }

    public async Task<IEnumerable<WarehouseDTO>> GetAllWarehousesAsync(CancellationToken ct = default)
    {
        var warehouses = await _warehouseRepository.GetAllActiveAsync(ct);
        return warehouses.Select(MapWarehouseToDto);
    }

    public async Task<WarehouseDTO> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating warehouse {WarehouseCode}", request.WarehouseCode);

        var warehouse = new Warehouse
        {
            WarehouseId = Guid.NewGuid(),
            WarehouseName = request.WarehouseName,
            WarehouseCode = request.WarehouseCode,
            Address = request.Address,
            Type = request.Type,
            IsActive = true,
            Priority = request.Priority,
            Capacity = request.Capacity,
            ContactDetails = request.ContactDetails,
            OperatingHours = request.OperatingHours,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _warehouseRepository.CreateAsync(warehouse, ct);
        return MapWarehouseToDto(created);
    }

    public async Task<WarehouseDTO> UpdateWarehouseAsync(UpdateWarehouseRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating warehouse {WarehouseId}", request.WarehouseId);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, ct)
            ?? throw new InvalidOperationException($"Warehouse {request.WarehouseId} not found");

        if (request.WarehouseName is not null) warehouse.WarehouseName = request.WarehouseName;
        if (request.Address is not null) warehouse.Address = request.Address;
        if (request.IsActive.HasValue) warehouse.IsActive = request.IsActive.Value;
        if (request.Priority.HasValue) warehouse.Priority = request.Priority.Value;
        if (request.Capacity.HasValue) warehouse.Capacity = request.Capacity;
        if (request.ContactDetails is not null) warehouse.ContactDetails = request.ContactDetails;
        if (request.OperatingHours is not null) warehouse.OperatingHours = request.OperatingHours;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _warehouseRepository.UpdateAsync(warehouse, ct);
        return MapWarehouseToDto(warehouse);
    }

    // Continued in Part 2...
    // Mapping helpers
    private static StockItemDTO MapToDto(StockItem item) => new()
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

    private static StockReservationDTO MapReservationToDto(StockReservation r) => new()
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
    };

    private static WarehouseDTO MapWarehouseToDto(Warehouse w) => new()
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

    // Placeholder implementations for remaining methods
    public Task<StockTransferDTO> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Transfer operations to be implemented");

    public Task<StockTransferDTO?> GetTransferAsync(Guid transferId, CancellationToken ct = default)
        => throw new NotImplementedException("Transfer operations to be implemented");

    public Task<IEnumerable<StockTransferDTO>> GetTransfersByWarehouseAsync(Guid warehouseId, bool isSource, CancellationToken ct = default)
        => throw new NotImplementedException("Transfer operations to be implemented");

    public Task<bool> ShipTransferAsync(ShipTransferRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Transfer operations to be implemented");

    public Task<bool> ReceiveTransferAsync(ReceiveTransferRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Transfer operations to be implemented");

    public async Task<IEnumerable<StockAlertDTO>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        var alerts = await _alertRepository.GetActiveAlertsAsync(ct);
        return alerts.Select(a => new StockAlertDTO
        {
            AlertId = a.AlertId,
            StockItemId = a.StockItemId,
            AlertType = a.AlertType,
            AlertStatus = a.AlertStatus,
            Message = a.Message,
            TriggeredAt = a.TriggeredAt,
            AcknowledgedAt = a.AcknowledgedAt,
            AcknowledgedBy = a.AcknowledgedBy,
            ResolvedAt = a.ResolvedAt
        });
    }

    public async Task<IEnumerable<StockAlertDTO>> GetAlertsByTypeAsync(string alertType, CancellationToken ct = default)
    {
        var alerts = await _alertRepository.GetByTypeAsync(alertType, ct);
        return alerts.Select(a => new StockAlertDTO
        {
            AlertId = a.AlertId,
            StockItemId = a.StockItemId,
            AlertType = a.AlertType,
            AlertStatus = a.AlertStatus,
            Message = a.Message,
            TriggeredAt = a.TriggeredAt
        });
    }

    public async Task<bool> AcknowledgeAlertAsync(AcknowledgeAlertRequest request, CancellationToken ct = default)
    {
        var result = await _alertRepository.AcknowledgeAsync(request.AlertId, request.AcknowledgedBy, ct);
        return result > 0;
    }

    public async Task<int> TriggerAlertsAsync(CancellationToken ct = default)
    {
        var lowStock = await _stockRepository.TriggerLowStockAlertsAsync(ct);
        var overStock = await _stockRepository.TriggerOverStockAlertsAsync(ct);
        return lowStock + overStock;
    }

    // Purchase Order stubs
    public Task<PurchaseOrderDTO> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("PO operations to be implemented");

    public Task<PurchaseOrderDTO?> GetPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken ct = default)
        => throw new NotImplementedException("PO operations to be implemented");

    public Task<IEnumerable<PurchaseOrderDTO>> GetPurchaseOrdersByStatusAsync(string status, CancellationToken ct = default)
        => throw new NotImplementedException("PO operations to be implemented");

    public Task<bool> ReceivePurchaseOrderAsync(ReceivePurchaseOrderRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("PO operations to be implemented");

    // Supplier stubs
    public Task<SupplierDTO?> GetSupplierAsync(Guid supplierId, CancellationToken ct = default)
        => throw new NotImplementedException("Supplier operations to be implemented");

    public Task<IEnumerable<SupplierDTO>> GetAllSuppliersAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Supplier operations to be implemented");

    public Task<SupplierDTO> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default)
        => throw new NotImplementedException("Supplier operations to be implemented");

    // Reports
    public Task<IEnumerable<StockValuationDTO>> GetStockValuationReportAsync(Guid? warehouseId = null, CancellationToken ct = default)
        => _reportRepository.GetStockValuationAsync(warehouseId, ct);

    public Task<IEnumerable<StockAgingSummaryDTO>> GetStockAgingReportAsync(CancellationToken ct = default)
        => _reportRepository.GetStockAgingSummaryAsync(ct);

    public Task<IEnumerable<InventoryTurnoverDTO>> GetInventoryTurnoverReportAsync(CancellationToken ct = default)
        => _reportRepository.GetInventoryTurnoverAsync(ct);

    public Task<IEnumerable<DeadStockDTO>> GetDeadStockReportAsync(int daysSinceLastMovement = 90, CancellationToken ct = default)
        => _reportRepository.GetDeadStockAsync(daysSinceLastMovement, ct);

    public Task<IEnumerable<LowStockReportDTO>> GetLowStockReportAsync(CancellationToken ct = default)
        => _reportRepository.GetLowStockReportAsync(ct);

    public Task<IEnumerable<ExpiryRiskDTO>> GetExpiryRiskReportAsync(int daysThreshold = 30, CancellationToken ct = default)
        => _reportRepository.GetExpiryRiskReportAsync(daysThreshold, ct);

    public Task<IEnumerable<ReorderRecommendationDTO>> GetReorderRecommendationsAsync(CancellationToken ct = default)
        => _reportRepository.GetReorderRecommendationsAsync(ct);

    public Task<IEnumerable<BackorderSummaryDTO>> GetBackorderSummaryAsync(CancellationToken ct = default)
        => _reportRepository.GetBackorderSummaryAsync(ct);

    public Task<IEnumerable<StockMovementDTO>> GetMovementHistoryAsync(Guid? stockItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
        => _reportRepository.GetMovementHistoryAsync(stockItemId, fromDate, toDate, ct);

    // Background Jobs
    public Task<int> ReleaseExpiredReservationsAsync(CancellationToken ct = default)
        => _stockRepository.ReleaseExpiredReservationsAsync(ct);

    public Task<int> ExpireStockBatchesAsync(CancellationToken ct = default)
        => _stockRepository.ExpireStockBatchesAsync(ct);

    public Task<int> RecalculateSafetyStockAsync(CancellationToken ct = default)
        => _stockRepository.RecalculateSafetyStockAsync(ct);
}
