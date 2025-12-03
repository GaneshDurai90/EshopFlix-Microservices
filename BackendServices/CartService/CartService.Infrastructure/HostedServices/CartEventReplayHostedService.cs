using CartService.Application.EventSourcing;
using CartService.Infrastructure.EventSourcing;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CartService.Infrastructure.HostedServices
{
    public sealed class CartEventReplayHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly bool _enabled;

        public CartEventReplayHostedService(IServiceProvider sp, IConfiguration cfg)
        {
            _sp = sp;
            _enabled = cfg.GetValue<bool?>("EventSourcing:ReplayOnStartup") ?? false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled) return;

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CartServiceDbContext>();
            var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var projector = new CartEventProjector(scope.ServiceProvider.GetRequiredService<ICartServiceDbContextProcedures>());

            var cartIds = await db.Carts.AsNoTracking().Select(c => c.Id).ToListAsync(stoppingToken);

            foreach (var cartId in cartIds)
            {
                try
                {
                    var events = await store.LoadAsync(cartId, stoppingToken);
                    foreach (var e in events)
                        await projector.ApplyAsync(e, stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Replay failed for CartId={CartId}", cartId);
                }
            }
        }
    }
}