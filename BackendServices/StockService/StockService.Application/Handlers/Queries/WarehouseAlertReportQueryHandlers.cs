using Microsoft.Extensions.Logging;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Queries;
using StockService.Application.Repositories;

namespace StockService.Application.Handlers.Queries;

// ============ Warehouse Query Handlers ============

public class GetWarehouseQueryHandler : IQueryHandler<GetWarehouseQuery, WarehouseDTO?>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<GetWarehouseQueryHandler> _logger;

    public GetWarehouseQueryHandler(IWarehouseRepository warehouseRepository, ILogger<GetWarehouseQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    public async Task<WarehouseDTO?> HandleAsync(GetWarehouseQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting warehouse {WarehouseId}", query.WarehouseId);
        var warehouse = await _warehouseRepository.GetByIdAsync(query.WarehouseId, ct);
        return warehouse is null ? null : new WarehouseDTO
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

public class GetWarehousesQueryHandler : IQueryHandler<GetWarehousesQuery, IEnumerable<WarehouseDTO>>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ILogger<GetWarehousesQueryHandler> _logger;

    public GetWarehousesQueryHandler(IWarehouseRepository warehouseRepository, ILogger<GetWarehousesQueryHandler> logger)
    {
        _warehouseRepository = warehouseRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<WarehouseDTO>> HandleAsync(GetWarehousesQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting all warehouses");
        var warehouses = await _warehouseRepository.GetAllActiveAsync(ct);
        return warehouses.Select(w => new WarehouseDTO
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
        });
    }
}

// ============ Alert Query Handlers ============

public class GetActiveAlertsQueryHandler : IQueryHandler<GetActiveAlertsQuery, IEnumerable<StockAlertDTO>>
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<GetActiveAlertsQueryHandler> _logger;

    public GetActiveAlertsQueryHandler(IAlertRepository alertRepository, ILogger<GetActiveAlertsQueryHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockAlertDTO>> HandleAsync(GetActiveAlertsQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting active alerts");
        var alerts = await _alertRepository.GetActiveAlertsAsync(ct);
        return alerts.Select(a => new StockAlertDTO
        {
            AlertId = a.AlertId,
            StockItemId = a.StockItemId,
            ProductId = a.StockItem?.ProductId,
            Sku = a.StockItem?.Sku,
            WarehouseName = a.StockItem?.Warehouse?.WarehouseName,
            AlertType = a.AlertType,
            AlertStatus = a.AlertStatus,
            Message = a.Message,
            TriggeredAt = a.TriggeredAt,
            AcknowledgedAt = a.AcknowledgedAt,
            AcknowledgedBy = a.AcknowledgedBy,
            ResolvedAt = a.ResolvedAt
        });
    }
}

public class GetAlertsByTypeQueryHandler : IQueryHandler<GetAlertsByTypeQuery, IEnumerable<StockAlertDTO>>
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<GetAlertsByTypeQueryHandler> _logger;

    public GetAlertsByTypeQueryHandler(IAlertRepository alertRepository, ILogger<GetAlertsByTypeQueryHandler> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<StockAlertDTO>> HandleAsync(GetAlertsByTypeQuery query, CancellationToken ct)
    {
        _logger.LogDebug("Getting alerts of type {AlertType}", query.AlertType);
        var alerts = await _alertRepository.GetByTypeAsync(query.AlertType, ct);
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
}

// ============ Report Query Handlers ============

public class GetStockValuationQueryHandler : IQueryHandler<GetStockValuationQuery, IEnumerable<StockValuationDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetStockValuationQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<StockValuationDTO>> HandleAsync(GetStockValuationQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetStockValuationAsync(query.WarehouseId, ct);
    }
}

public class GetStockAgingQueryHandler : IQueryHandler<GetStockAgingQuery, IEnumerable<StockAgingSummaryDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetStockAgingQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<StockAgingSummaryDTO>> HandleAsync(GetStockAgingQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetStockAgingSummaryAsync(ct);
    }
}

public class GetInventoryTurnoverQueryHandler : IQueryHandler<GetInventoryTurnoverQuery, IEnumerable<InventoryTurnoverDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetInventoryTurnoverQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<InventoryTurnoverDTO>> HandleAsync(GetInventoryTurnoverQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetInventoryTurnoverAsync(ct);
    }
}

public class GetDeadStockQueryHandler : IQueryHandler<GetDeadStockQuery, IEnumerable<DeadStockDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetDeadStockQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<DeadStockDTO>> HandleAsync(GetDeadStockQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetDeadStockAsync(query.DaysSinceLastMovement, ct);
    }
}

public class GetLowStockQueryHandler : IQueryHandler<GetLowStockQuery, IEnumerable<LowStockReportDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetLowStockQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<LowStockReportDTO>> HandleAsync(GetLowStockQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetLowStockReportAsync(ct);
    }
}

public class GetExpiryRiskQueryHandler : IQueryHandler<GetExpiryRiskQuery, IEnumerable<ExpiryRiskDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetExpiryRiskQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<ExpiryRiskDTO>> HandleAsync(GetExpiryRiskQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetExpiryRiskReportAsync(query.DaysThreshold, ct);
    }
}

public class GetReorderRecommendationsQueryHandler : IQueryHandler<GetReorderRecommendationsQuery, IEnumerable<ReorderRecommendationDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetReorderRecommendationsQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<ReorderRecommendationDTO>> HandleAsync(GetReorderRecommendationsQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetReorderRecommendationsAsync(ct);
    }
}

public class GetBackorderSummaryQueryHandler : IQueryHandler<GetBackorderSummaryQuery, IEnumerable<BackorderSummaryDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetBackorderSummaryQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<BackorderSummaryDTO>> HandleAsync(GetBackorderSummaryQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetBackorderSummaryAsync(ct);
    }
}

public class GetMovementHistoryQueryHandler : IQueryHandler<GetMovementHistoryQuery, IEnumerable<StockMovementDTO>>
{
    private readonly IReportRepository _reportRepository;

    public GetMovementHistoryQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<IEnumerable<StockMovementDTO>> HandleAsync(GetMovementHistoryQuery query, CancellationToken ct)
    {
        return await _reportRepository.GetMovementHistoryAsync(query.StockItemId, query.FromDate, query.ToDate, ct);
    }
}
