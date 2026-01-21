using Microsoft.AspNetCore.Mvc;
using StockService.Application.Commands;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Queries;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Aliases to avoid conflicts with API Contracts
using ApiStock = StockService.API.Contracts.Stock;
using ApiWarehouse = StockService.API.Contracts.Warehouse;
using ApiAlert = StockService.API.Contracts.Alert;

namespace StockService.API.Controllers
{
    [Route("api/Stock/[action]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IDispatcher _dispatcher;
        private readonly ILogger<StockController> _logger;

        public StockController(IDispatcher dispatcher, ILogger<StockController> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        // ============ Idempotency Helpers ============
        private string GetIdempotencyKey(object? body = null)
        {
            var key = Request.Headers["x-idempotency-key"].FirstOrDefault()
                   ?? Request.Headers["Idempotency-Key"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(key)) return key;

            var payload = body is null ? string.Empty : JsonSerializer.Serialize(body);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{Request.Method}:{Request.Path}|{payload}"));
            return Convert.ToHexString(bytes);
        }

        // ============ Stock Item Queries (CQRS) ============

        /// <summary>Get stock item by ID</summary>
        [HttpGet("{stockItemId:guid}")]
        public async Task<IActionResult> GetStockItem(Guid stockItemId, CancellationToken ct)
        {
            var item = await _dispatcher.QueryAsync(new GetStockItemQuery(stockItemId), ct);
            return item is null ? NotFound() : Ok(item);
        }

        /// <summary>Get all stock for a product across warehouses</summary>
        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetStockByProduct(Guid productId, [FromQuery] Guid? variationId, CancellationToken ct)
        {
            var items = await _dispatcher.QueryAsync(new GetStockByProductQuery(productId, variationId), ct);
            return Ok(items);
        }

        /// <summary>Get stock summary for a product</summary>
        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetStockSummary(Guid productId, [FromQuery] Guid? variationId, CancellationToken ct)
        {
            var summary = await _dispatcher.QueryAsync(new GetStockSummaryQuery(productId, variationId), ct);
            return summary is null ? NotFound() : Ok(summary);
        }

        /// <summary>Get all stock in a warehouse</summary>
        [HttpGet("{warehouseId:guid}")]
        public async Task<IActionResult> GetStockByWarehouse(Guid warehouseId, CancellationToken ct)
        {
            var items = await _dispatcher.QueryAsync(new GetStockByWarehouseQuery(warehouseId), ct);
            return Ok(items);
        }

        // ============ Stock Item Management (CQRS) ============

        /// <summary>Create a new stock item in a warehouse</summary>
        [HttpPost]
        public async Task<IActionResult> CreateStockItem([FromBody] ApiStock.CreateStockItemRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("Create stock item command: Product {ProductId} in Warehouse {WarehouseId}",
                request.ProductId, request.WarehouseId);

            var stockItem = await _dispatcher.SendAsync(new CreateStockItemCommand(
                request.ProductId,
                request.VariationId,
                request.WarehouseId,
                request.Sku,
                request.InitialQuantity,
                request.MinimumStockLevel,
                request.MaximumStockLevel,
                request.ReorderQuantity,
                request.UnitCost,
                request.ExpiryDate,
                request.BatchNumber,
                request.BinLocation,
                GetIdempotencyKey(request)
            ), ct);

            return CreatedAtAction(nameof(GetStockItem), new { stockItemId = stockItem.StockItemId }, stockItem);
        }

        /// <summary>Update stock item settings</summary>
        [HttpPut]
        public async Task<IActionResult> UpdateStockItem([FromBody] ApiStock.UpdateStockItemRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("Update stock item command: {StockItemId}", request.StockItemId);

            var stockItem = await _dispatcher.SendAsync(new UpdateStockItemCommand(
                request.StockItemId,
                request.MinimumStockLevel,
                request.MaximumStockLevel,
                request.ReorderQuantity,
                request.UnitCost,
                request.BinLocation,
                request.IsActive,
                GetIdempotencyKey(request)
            ), ct);

            return Ok(stockItem);
        }

        // ============ Availability Queries (CQRS) ============

        /// <summary>Check if product is available</summary>
        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetAvailability(Guid productId, [FromQuery] Guid? variationId, CancellationToken ct)
        {
            var availability = await _dispatcher.QueryAsync(new GetAvailabilityQuery(productId, variationId), ct);
            
            // Return "out of stock" response instead of 404 when no stock data exists
            if (availability is null)
            {
                return Ok(new StockAvailabilityDTO
                {
                    ProductId = productId,
                    VariationId = variationId,
                    TotalAvailable = 0,
                    TotalReserved = 0,
                    IsInStock = false,
                    IsLowStock = false,
                    WarehouseBreakdown = new List<WarehouseStockDTO>()
                });
            }
            
            return Ok(availability);
        }

        /// <summary>Check availability and get allocation suggestions</summary>
        [HttpPost]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckAvailabilityRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            var response = await _dispatcher.QueryAsync(
                new CheckAvailabilityQuery(request.ProductId, request.VariationId, request.Quantity, request.PreferredWarehouseId), ct);
            return Ok(response);
        }

        // ============ Reservations Commands (CQRS) ============

        /// <summary>Reserve stock for cart/order</summary>
        [HttpPost]
        public async Task<IActionResult> ReserveStock([FromBody] CreateReservationRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("Reserve stock command: Product {ProductId}, Qty {Quantity}, Cart {CartId}",
                request.ProductId, request.Quantity, request.CartId);

            var response = await _dispatcher.SendAsync(new ReserveStockCommand(
                request.ProductId,
                request.VariationId,
                request.WarehouseId,
                request.CartId,
                request.OrderId,
                request.CustomerId,
                request.Quantity,
                request.ReservationType,
                request.TtlMinutes,
                GetIdempotencyKey(request)
            ), ct);

            if (!response.Success)
            {
                _logger.LogWarning("Reservation failed: {Message}", response.Message);
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>Commit reservation when order is placed</summary>
        [HttpPost]
        public async Task<IActionResult> CommitReservation([FromBody] CommitReservationRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Commit reservation command: {ReservationId} for Order {OrderId}",
                request.ReservationId, request.OrderId);

            var success = await _dispatcher.SendAsync(new CommitReservationCommand(
                request.ReservationId,
                request.OrderId,
                GetIdempotencyKey(request)
            ), ct);

            return success ? Ok() : NotFound();
        }

        /// <summary>Release a specific reservation</summary>
        [HttpPost]
        public async Task<IActionResult> ReleaseReservation([FromBody] ReleaseReservationRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Release reservation command: {ReservationId}", request.ReservationId);
            
            var success = await _dispatcher.SendAsync(new ReleaseReservationCommand(
                request.ReservationId,
                request.Reason,
                GetIdempotencyKey(request)
            ), ct);

            return success ? Ok() : NotFound();
        }

        /// <summary>Release all reservations for a cart (cart abandoned/cleared)</summary>
        [HttpPost("{cartId:guid}")]
        public async Task<IActionResult> ReleaseCartReservations(Guid cartId, CancellationToken ct)
        {
            _logger.LogInformation("Release cart reservations command: Cart {CartId}", cartId);
            
            var count = await _dispatcher.SendAsync(new ReleaseCartReservationsCommand(
                cartId,
                GetIdempotencyKey()
            ), ct);

            return Ok(new { released = count });
        }

        /// <summary>Get reservations for a cart</summary>
        [HttpGet("{cartId:guid}")]
        public async Task<IActionResult> GetCartReservations(Guid cartId, CancellationToken ct)
        {
            var reservations = await _dispatcher.QueryAsync(new GetCartReservationsQuery(cartId), ct);
            return Ok(reservations);
        }

        /// <summary>Get reservations for an order</summary>
        [HttpGet("{orderId:guid}")]
        public async Task<IActionResult> GetOrderReservations(Guid orderId, CancellationToken ct)
        {
            var reservations = await _dispatcher.QueryAsync(new GetOrderReservationsQuery(orderId), ct);
            return Ok(reservations);
        }

        // ============ Stock Adjustments Commands (CQRS) ============

        /// <summary>Increase stock quantity</summary>
        [HttpPost]
        public async Task<IActionResult> IncreaseStock([FromBody] ApiStock.IncreaseStockRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Increase stock command: {StockItemId} by {Quantity}", request.StockItemId, request.Quantity);
            
            var adjustment = await _dispatcher.SendAsync(new IncreaseStockCommand(
                request.StockItemId,
                request.Quantity,
                request.Reason,
                request.PerformedBy,
                GetIdempotencyKey(request)
            ), ct);

            return Ok(adjustment);
        }

        /// <summary>Decrease stock quantity</summary>
        [HttpPost]
        public async Task<IActionResult> DecreaseStock([FromBody] ApiStock.DecreaseStockRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Decrease stock command: {StockItemId} by {Quantity}", request.StockItemId, request.Quantity);
            
            var adjustment = await _dispatcher.SendAsync(new DecreaseStockCommand(
                request.StockItemId,
                request.Quantity,
                request.Reason,
                request.PerformedBy,
                GetIdempotencyKey(request)
            ), ct);

            return Ok(adjustment);
        }

        /// <summary>Manual stock adjustment</summary>
        [HttpPost]
        public async Task<IActionResult> AdjustStock([FromBody] CreateAdjustmentRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Adjust stock command: {StockItemId}, Type {Type}, Qty {Quantity}",
                request.StockItemId, request.AdjustmentType, request.AdjustmentQuantity);
            
            var adjustment = await _dispatcher.SendAsync(new AdjustStockCommand(
                request.StockItemId,
                request.AdjustmentType,
                request.AdjustmentQuantity,
                request.Reason,
                request.Notes,
                request.PerformedBy,
                request.ApprovedBy,
                GetIdempotencyKey(request)
            ), ct);

            return Ok(adjustment);
        }

        // ============ Warehouses (CQRS) ============

        /// <summary>Get warehouse by ID</summary>
        [HttpGet("{warehouseId:guid}")]
        public async Task<IActionResult> GetWarehouse(Guid warehouseId, CancellationToken ct)
        {
            var warehouse = await _dispatcher.QueryAsync(new GetWarehouseQuery(warehouseId), ct);
            return warehouse is null ? NotFound() : Ok(warehouse);
        }

        /// <summary>Get all warehouses</summary>
        [HttpGet]
        public async Task<IActionResult> GetWarehouses(CancellationToken ct)
        {
            var warehouses = await _dispatcher.QueryAsync(new GetWarehousesQuery(), ct);
            return Ok(warehouses);
        }

        /// <summary>Create new warehouse</summary>
        [HttpPost]
        public async Task<IActionResult> CreateWarehouse([FromBody] ApiWarehouse.CreateWarehouseRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("Create warehouse command: {WarehouseCode}", request.WarehouseCode);
            
            var warehouse = await _dispatcher.SendAsync(new CreateWarehouseCommand(
                request.WarehouseName,
                request.WarehouseCode,
                request.Address,
                request.Type,
                request.Priority,
                request.Capacity,
                request.ContactDetails,
                request.OperatingHours,
                GetIdempotencyKey(request)
            ), ct);

            return CreatedAtAction(nameof(GetWarehouse), new { warehouseId = warehouse.WarehouseId }, warehouse);
        }

        /// <summary>Update warehouse</summary>
        [HttpPut]
        public async Task<IActionResult> UpdateWarehouse([FromBody] ApiWarehouse.UpdateWarehouseRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("Update warehouse command: {WarehouseId}", request.WarehouseId);
            
            var warehouse = await _dispatcher.SendAsync(new UpdateWarehouseCommand(
                request.WarehouseId,
                request.WarehouseName,
                request.Address,
                request.IsActive,
                request.Priority,
                request.Capacity,
                request.ContactDetails,
                request.OperatingHours,
                GetIdempotencyKey(request)
            ), ct);

            return Ok(warehouse);
        }

        // ============ Alerts (CQRS) ============

        /// <summary>Get all active alerts</summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveAlerts(CancellationToken ct)
        {
            var alerts = await _dispatcher.QueryAsync(new GetActiveAlertsQuery(), ct);
            return Ok(alerts);
        }

        /// <summary>Get alerts by type</summary>
        [HttpGet("{alertType}")]
        public async Task<IActionResult> GetAlertsByType(string alertType, CancellationToken ct)
        {
            var alerts = await _dispatcher.QueryAsync(new GetAlertsByTypeQuery(alertType), ct);
            return Ok(alerts);
        }

        /// <summary>Acknowledge an alert</summary>
        [HttpPost]
        public async Task<IActionResult> AcknowledgeAlert([FromBody] ApiAlert.AcknowledgeAlertRequest request, CancellationToken ct)
        {
            var success = await _dispatcher.SendAsync(new AcknowledgeAlertCommand(
                request.AlertId,
                request.AcknowledgedBy,
                GetIdempotencyKey(request)
            ), ct);

            return success ? Ok() : NotFound();
        }

        /// <summary>Trigger alert checks (low stock, overstock)</summary>
        [HttpPost]
        public async Task<IActionResult> TriggerAlerts(CancellationToken ct)
        {
            var count = await _dispatcher.SendAsync(new TriggerAlertsCommand(GetIdempotencyKey()), ct);
            return Ok(new { alertsTriggered = count });
        }

        // ============ Reports (CQRS Queries) ============

        /// <summary>Get stock valuation report</summary>
        [HttpGet]
        public async Task<IActionResult> GetStockValuation([FromQuery] Guid? warehouseId, CancellationToken ct)
        {
            var report = await _dispatcher.QueryAsync(new GetStockValuationQuery(warehouseId), ct);
            return Ok(report);
        }

        /// <summary>Get stock aging report</summary>
        [HttpGet]
        public async Task<IActionResult> GetStockAging(CancellationToken ct)
        {
            var report = await _dispatcher.QueryAsync(new GetStockAgingQuery(), ct);
            return Ok(report);
        }

        /// <summary>Get inventory turnover report</summary>
        [HttpGet]
        public async Task<IActionResult> GetInventoryTurnover(CancellationToken ct)
        {
            var report = await _dispatcher.QueryAsync(new GetInventoryTurnoverQuery(), ct);
            return Ok(report);
        }

        /// <summary>Get dead stock report</summary>
        [HttpGet]
        public async Task<IActionResult> GetDeadStock([FromQuery] int days = 90, CancellationToken ct = default)
        {
            var report = await _dispatcher.QueryAsync(new GetDeadStockQuery(days), ct);
            return Ok(report);
        }

        /// <summary>Get low stock report</summary>
        [HttpGet]
        public async Task<IActionResult> GetLowStock(CancellationToken ct)
        {
            var report = await _dispatcher.QueryAsync(new GetLowStockQuery(), ct);
            return Ok(report);
        }

        /// <summary>Get expiry risk report</summary>
        [HttpGet]
        public async Task<IActionResult> GetExpiryRisk([FromQuery] int days = 30, CancellationToken ct = default)
        {
            var report = await _dispatcher.QueryAsync(new GetExpiryRiskQuery(days), ct);
            return Ok(report);
        }

        /// <summary>Get reorder recommendations</summary>
        [HttpGet]
        public async Task<IActionResult> GetReorderRecommendations(CancellationToken ct)
        {
            var report = await _dispatcher.QueryAsync(new GetReorderRecommendationsQuery(), ct);
            return Ok(report);
        }

        /// <summary>Get backorder summary</summary>
        [HttpGet]
        public async Task<IActionResult> GetBackorderSummary(CancellationToken ct)
        {
            var report = await _dispatcher.QueryAsync(new GetBackorderSummaryQuery(), ct);
            return Ok(report);
        }

        /// <summary>Get movement history</summary>
        [HttpGet]
        public async Task<IActionResult> GetMovementHistory(
            [FromQuery] Guid? stockItemId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct)
        {
            var history = await _dispatcher.QueryAsync(new GetMovementHistoryQuery(stockItemId, fromDate, toDate), ct);
            return Ok(history);
        }

        // ============ Background Jobs Commands (CQRS) ============

        /// <summary>Release expired reservations (called by scheduler)</summary>
        [HttpPost]
        public async Task<IActionResult> ReleaseExpiredReservations(CancellationToken ct)
        {
            var count = await _dispatcher.SendAsync(new ReleaseExpiredReservationsCommand(), ct);
            return Ok(new { released = count });
        }

        /// <summary>Expire stock batches (called by scheduler)</summary>
        [HttpPost]
        public async Task<IActionResult> ExpireStockBatches(CancellationToken ct)
        {
            var count = await _dispatcher.SendAsync(new ExpireStockBatchesCommand(), ct);
            return Ok(new { expired = count });
        }

        /// <summary>Recalculate safety stock levels (called by scheduler)</summary>
        [HttpPost]
        public async Task<IActionResult> RecalculateSafetyStock(CancellationToken ct)
        {
            var count = await _dispatcher.SendAsync(new RecalculateSafetyStockCommand(), ct);
            return Ok(new { updated = count });
        }

        // ============ Data Initialization ============

        /// <summary>
        /// Bulk create stock items for multiple products.
        /// Use this to initialize stock data for existing catalog products.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkCreateStockItems([FromBody] BulkCreateStockItemsRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Bulk create stock items: {Count} items", request.Items?.Count ?? 0);

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { error = "No items provided" });
            }

            var results = new List<StockItemDTO>();
            var errors = new List<object>();

            foreach (var item in request.Items)
            {
                try
                {
                    var stockItem = await _dispatcher.SendAsync(new CreateStockItemCommand(
                        item.ProductId,
                        item.VariationId,
                        item.WarehouseId,
                        item.Sku,
                        item.InitialQuantity,
                        item.MinimumStockLevel,
                        item.MaximumStockLevel,
                        item.ReorderQuantity,
                        item.UnitCost,
                        item.ExpiryDate,
                        item.BatchNumber,
                        item.BinLocation,
                        null
                    ), ct);

                    results.Add(stockItem);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create stock item for product {ProductId}", item.ProductId);
                    errors.Add(new { productId = item.ProductId, error = ex.Message });
                }
            }

            return Ok(new
            {
                created = results.Count,
                failed = errors.Count,
                items = results,
                errors = errors
            });
        }
    }

    /// <summary>
    /// Request for bulk creating stock items.
    /// </summary>
    public record BulkCreateStockItemsRequest
    {
        public List<ApiStock.CreateStockItemRequest> Items { get; init; } = new();
    }
}
