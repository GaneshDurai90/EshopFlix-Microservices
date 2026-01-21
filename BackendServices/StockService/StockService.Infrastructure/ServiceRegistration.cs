using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockService.Application.Commands;
using StockService.Application.CQRS;
using StockService.Application.DTOs;
using StockService.Application.Handlers.Commands;
using StockService.Application.Handlers.Queries;
using StockService.Application.Messaging;
using StockService.Application.Queries;
using StockService.Application.Repositories;
using StockService.Application.Services.Abstractions;
using StockService.Application.Services.Implementations;
using StockService.Infrastructure.Messaging;
using StockService.Infrastructure.Persistence;
using StockService.Infrastructure.Repositories;
using StockService.Infrastructure.Services;

namespace StockService.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Memory Cache for idempotency caching layer
            services.AddMemoryCache();

            // Database Context
            var connectionString = configuration.GetConnectionString("DbConnection");
            services.AddDbContext<StockServiceDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                });
            });

            // Register stored procedures interface via the Procedures property
            services.AddScoped<IStockServiceDbContextProcedures>(sp => 
                sp.GetRequiredService<StockServiceDbContext>().Procedures);

            // Idempotency - Database-backed with memory cache layer
            services.AddScoped<IIdempotentRequestStore, IdempotentRequestStore>();
            services.AddScoped<IIdempotencyService, DatabaseIdempotencyService>();

            // Repositories
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<IAlertRepository, AlertRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<ITransferRepository, TransferRepository>();
            services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

            // CQRS Dispatcher
            services.AddScoped<IDispatcher, Dispatcher>();

            // Command Handlers - Reservations
            services.AddScoped<ICommandHandler<ReserveStockCommand, CreateReservationResponse>, ReserveStockCommandHandler>();
            services.AddScoped<ICommandHandler<CommitReservationCommand, bool>, CommitReservationCommandHandler>();
            services.AddScoped<ICommandHandler<ReleaseReservationCommand, bool>, ReleaseReservationCommandHandler>();
            services.AddScoped<ICommandHandler<ReleaseCartReservationsCommand, int>, ReleaseCartReservationsCommandHandler>();

            // Command Handlers - Adjustments
            services.AddScoped<ICommandHandler<IncreaseStockCommand, StockAdjustmentDTO>, IncreaseStockCommandHandler>();
            services.AddScoped<ICommandHandler<DecreaseStockCommand, StockAdjustmentDTO>, DecreaseStockCommandHandler>();
            services.AddScoped<ICommandHandler<AdjustStockCommand, StockAdjustmentDTO>, AdjustStockCommandHandler>();

            // Command Handlers - Stock Items & Warehouses
            services.AddScoped<ICommandHandler<CreateStockItemCommand, StockItemDTO>, CreateStockItemCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateStockItemCommand, StockItemDTO>, UpdateStockItemCommandHandler>();
            services.AddScoped<ICommandHandler<CreateWarehouseCommand, WarehouseDTO>, CreateWarehouseCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateWarehouseCommand, WarehouseDTO>, UpdateWarehouseCommandHandler>();

            // Command Handlers - Alerts & Background Jobs
            services.AddScoped<ICommandHandler<AcknowledgeAlertCommand, bool>, AcknowledgeAlertCommandHandler>();
            services.AddScoped<ICommandHandler<TriggerAlertsCommand, int>, TriggerAlertsCommandHandler>();
            services.AddScoped<ICommandHandler<ReleaseExpiredReservationsCommand, int>, ReleaseExpiredReservationsCommandHandler>();
            services.AddScoped<ICommandHandler<ExpireStockBatchesCommand, int>, ExpireStockBatchesCommandHandler>();
            services.AddScoped<ICommandHandler<RecalculateSafetyStockCommand, int>, RecalculateSafetyStockCommandHandler>();

            // Query Handlers - Stock Items
            services.AddScoped<IQueryHandler<GetStockItemQuery, StockItemDTO?>, GetStockItemQueryHandler>();
            services.AddScoped<IQueryHandler<GetStockByProductQuery, IEnumerable<StockItemDTO>>, GetStockByProductQueryHandler>();
            services.AddScoped<IQueryHandler<GetStockSummaryQuery, StockItemSummaryDTO?>, GetStockSummaryQueryHandler>();
            services.AddScoped<IQueryHandler<GetStockByWarehouseQuery, IEnumerable<StockItemDTO>>, GetStockByWarehouseQueryHandler>();

            // Query Handlers - Availability
            services.AddScoped<IQueryHandler<GetAvailabilityQuery, StockAvailabilityDTO?>, GetAvailabilityQueryHandler>();
            services.AddScoped<IQueryHandler<CheckAvailabilityQuery, CheckAvailabilityResponse>, CheckAvailabilityQueryHandler>();

            // Query Handlers - Reservations
            services.AddScoped<IQueryHandler<GetCartReservationsQuery, IEnumerable<StockReservationDTO>>, GetCartReservationsQueryHandler>();
            services.AddScoped<IQueryHandler<GetOrderReservationsQuery, IEnumerable<StockReservationDTO>>, GetOrderReservationsQueryHandler>();

            // Query Handlers - Warehouses
            services.AddScoped<IQueryHandler<GetWarehouseQuery, WarehouseDTO?>, GetWarehouseQueryHandler>();
            services.AddScoped<IQueryHandler<GetWarehousesQuery, IEnumerable<WarehouseDTO>>, GetWarehousesQueryHandler>();

            // Query Handlers - Alerts
            services.AddScoped<IQueryHandler<GetActiveAlertsQuery, IEnumerable<StockAlertDTO>>, GetActiveAlertsQueryHandler>();
            services.AddScoped<IQueryHandler<GetAlertsByTypeQuery, IEnumerable<StockAlertDTO>>, GetAlertsByTypeQueryHandler>();

            // Query Handlers - Reports
            services.AddScoped<IQueryHandler<GetStockValuationQuery, IEnumerable<StockValuationDTO>>, GetStockValuationQueryHandler>();
            services.AddScoped<IQueryHandler<GetStockAgingQuery, IEnumerable<StockAgingSummaryDTO>>, GetStockAgingQueryHandler>();
            services.AddScoped<IQueryHandler<GetInventoryTurnoverQuery, IEnumerable<InventoryTurnoverDTO>>, GetInventoryTurnoverQueryHandler>();
            services.AddScoped<IQueryHandler<GetDeadStockQuery, IEnumerable<DeadStockDTO>>, GetDeadStockQueryHandler>();
            services.AddScoped<IQueryHandler<GetLowStockQuery, IEnumerable<LowStockReportDTO>>, GetLowStockQueryHandler>();
            services.AddScoped<IQueryHandler<GetExpiryRiskQuery, IEnumerable<ExpiryRiskDTO>>, GetExpiryRiskQueryHandler>();
            services.AddScoped<IQueryHandler<GetReorderRecommendationsQuery, IEnumerable<ReorderRecommendationDTO>>, GetReorderRecommendationsQueryHandler>();
            services.AddScoped<IQueryHandler<GetBackorderSummaryQuery, IEnumerable<BackorderSummaryDTO>>, GetBackorderSummaryQueryHandler>();
            services.AddScoped<IQueryHandler<GetMovementHistoryQuery, IEnumerable<StockMovementDTO>>, GetMovementHistoryQueryHandler>();

            // Event Publisher (Outbox Pattern)
            services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

            // Application Services (kept for backward compatibility)
            services.AddScoped<IStockAppService, StockAppService>();
        }
    }
}
