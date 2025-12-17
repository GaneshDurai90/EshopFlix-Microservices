using AutoMapper;
using CartService.Application.Carts.Commands;
using CartService.Application.Carts.Handlers;


//using CartService.API.Eventing;
using CartService.Application.CQRS;
using CartService.Application.DTOs;
using CartService.Application.EventSourcing;
using CartService.Application.Mappers;
using CartService.Application.Messaging;
using CartService.Application.Repositories;
using CartService.Application.Services.Abstractions;
using CartService.Application.Services.Implementations;
using CartService.Application.Snapshots;
using CartService.Infrastructure.EventSourcing;
using CartService.Infrastructure.HostedServices;
using CartService.Infrastructure.Messaging;
//using CartService.Infrastructure.Outbox;
using CartService.Infrastructure.Persistence;
using CartService.Infrastructure.Persistence.Repositories;
using CartService.Infrastructure.Persistence.Services.Abstractions;
using CartService.Infrastructure.Persistence.Services.Implementations;
using CartService.Infrastructure.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CartService.Infrastructure
{
    public class ServiceRegistration
    {
        public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // DbContext (factory-backed so scoped contexts come from pooled creators)
            var connectionString = configuration.GetConnectionString("DbConnection");
            services.AddDbContextFactory<CartServiceDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<CartServiceDbContext>>().CreateDbContext());

            // Repositories
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ICartServiceDbContextProcedures, CartServiceDbContextProcedures>();

            // Application Services
            services.AddScoped<ICartAppService, CartAppService>();

            // AutoMapper
            services.AddAutoMapper(cfg => cfg.AddProfile<CartMapper>());

            // Idempotency wiring
            services.AddMemoryCache();

            // DB-backed idempotent request store (persists request metadata)
            services.AddScoped<IIdempotentRequest, IdempotencyService>();

            // Application adapter + orchestrator used by controllers
            services.AddScoped<IIdempotentAppRequest>(sp =>
                new IdempotentRequestAdapter(sp.GetRequiredService<IIdempotentRequest>()));
            services.AddScoped<IIdempotencyAppService, IdempotencyAppService>();

            // If another component still consumes the in-memory coordinator, keep this. Otherwise remove to avoid confusion.
            // services.AddScoped<IIdempotencyService, InMemoryIdempotencyCoordinator>();

            // CQRS
            services.AddScoped<IDispatcher, Dispatcher>();

            // Event Store (Application abstraction + Infra implementation)
            services.AddScoped<IEventStore, SqlEventStore>();

            // Outbox and Broker
            services.AddScoped<IOutboxPublisher, OutboxPublisher>();
            services.AddSingleton<IBrokerPublisher, LoggingBrokerPublisher>();
            services.AddHostedService<OutboxDispatcherHostedService>();

            // Snapshots
            services.AddSingleton<ISnapshotPolicy, ModuloSnapshotPolicy>();
            services.AddScoped<ISnapshotWriter, EventStoreSnapshotWriter>();

            // Command Handlers
            services.AddScoped<ICommandHandler<AddItemCommand, CartDTO>, AddItemCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateQuantityCommand, int>, UpdateQuantityCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteItemCommand, int>, DeleteItemCommandHandler>();
            services.AddScoped<ICommandHandler<ApplyCouponCommand, bool>, ApplyCouponCommandHandler>();
            services.AddScoped<ICommandHandler<RemoveCouponCommand, bool>, RemoveCouponCommandHandler>();
            services.AddScoped<ICommandHandler<SelectShippingCommand, bool>, SelectShippingCommandHandler>();
            services.AddScoped<ICommandHandler<RecalculateTotalsCommand, bool>, RecalculateTotalsCommandHandler>();
            services.AddScoped<ICommandHandler<ClearCartCommand, bool>, ClearCartCommandHandler>();
            services.AddScoped<ICommandHandler<SaveForLaterCommand, bool>, SaveForLaterCommandHandler>();
            services.AddScoped<ICommandHandler<MoveSavedToCartCommand, bool>, MoveSavedToCartCommandHandler>();
            services.AddScoped<ICommandHandler<SetPaymentCommand, bool>, SetPaymentCommandHandler>();
            services.AddScoped<ICommandHandler<MakeInActiveCommand, bool>, MakeInActiveCommandHandler>();

            // Replayer (enable via config: EventSourcing:ReplayOnStartup = true)
            services.AddHostedService<CartEventReplayHostedService>();
        }
    }
}
