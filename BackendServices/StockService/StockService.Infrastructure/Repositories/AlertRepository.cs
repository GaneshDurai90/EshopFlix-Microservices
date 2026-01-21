using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly StockServiceDbContext _db;
    private readonly ILogger<AlertRepository> _logger;

    public AlertRepository(StockServiceDbContext db, ILogger<AlertRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<StockAlert?> GetByIdAsync(Guid alertId, CancellationToken ct = default)
    {
        return await _db.StockAlerts
            .Include(a => a.StockItem)
            .ThenInclude(s => s.Warehouse)
            .FirstOrDefaultAsync(a => a.AlertId == alertId, ct);
    }

    public async Task<IEnumerable<StockAlert>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        return await _db.StockAlerts
            .Include(a => a.StockItem)
            .ThenInclude(s => s.Warehouse)
            .Where(a => a.AlertStatus == "Active")
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockAlert>> GetByTypeAsync(string alertType, CancellationToken ct = default)
    {
        return await _db.StockAlerts
            .Include(a => a.StockItem)
            .Where(a => a.AlertType == alertType && a.AlertStatus == "Active")
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StockAlert>> GetByStockItemAsync(Guid stockItemId, CancellationToken ct = default)
    {
        return await _db.StockAlerts
            .Where(a => a.StockItemId == stockItemId)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(ct);
    }

    public async Task<StockAlert> CreateAsync(StockAlert alert, CancellationToken ct = default)
    {
        _db.StockAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created alert {AlertId}: {AlertType}", alert.AlertId, alert.AlertType);
        return alert;
    }

    public async Task UpdateAsync(StockAlert alert, CancellationToken ct = default)
    {
        _db.Entry(alert).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> AcknowledgeAsync(Guid alertId, Guid acknowledgedBy, CancellationToken ct = default)
    {
        var alert = await GetByIdAsync(alertId, ct);
        if (alert is null || alert.AlertStatus != "Active") return 0;

        alert.AlertStatus = "Acknowledged";
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedBy = acknowledgedBy;

        _logger.LogInformation("Alert {AlertId} acknowledged by {UserId}", alertId, acknowledgedBy);
        return await _db.SaveChangesAsync(ct);
    }

    public async Task<int> ResolveAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await GetByIdAsync(alertId, ct);
        if (alert is null) return 0;

        alert.AlertStatus = "Resolved";
        alert.ResolvedAt = DateTime.UtcNow;

        _logger.LogInformation("Alert {AlertId} resolved", alertId);
        return await _db.SaveChangesAsync(ct);
    }
}
