using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CartService.Infrastructure.Messaging
{
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly string _instanceId = Guid.NewGuid().ToString("N");
        private readonly ILogger _log;

        public OutboxDispatcher(IServiceProvider sp)
        {
            _sp = sp;
            _log = Log.ForContext<OutboxDispatcher>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Information("OutboxDispatcher started (instance={Instance})", _instanceId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<CartServiceDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                    var now = DateTime.UtcNow;
                    var candidates = await db.OutboxMessages
                        .Where(m => !m.Processed && (m.LockedAt == null || m.LockedAt < now.AddMinutes(-5)))
                        .OrderBy(m => m.OccurredOn)
                        .Take(25)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in candidates)
                    {
                        // Try to claim lock (simple lease)
                        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE dbo.OutboxMessages
                               SET LockedBy = {_instanceId}, LockedAt = {now}
                               WHERE Id = {msg.Id} AND (LockedAt IS NULL OR LockedAt < {now.AddMinutes(-5)})",
                            cancellationToken: stoppingToken);

                        if (affected == 0) continue; // someone else locked it

                        var locked = await db.OutboxMessages.FindAsync(new object[] { msg.Id }, cancellationToken: stoppingToken);

                        try
                        {
                            var dest = locked.Destination ?? "topic:eshop.events";
                            await publisher.PublishAsync(dest, locked.Type, locked.MessageId, locked.Content, stoppingToken);

                            locked.Processed = true;
                            locked.ProcessedOn = DateTime.UtcNow;
                            db.OutboxMessages.Update(locked);
                            await db.SaveChangesAsync(stoppingToken);

                            _log.Information("Outbox message {Id} marked processed", locked.Id);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Failed to publish outbox message {Id}", locked.Id);
                            locked.LockedAt = null;
                            locked.LockedBy = null;
                            locked.RetryCount++;
                            db.OutboxMessages.Update(locked);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ForContext<OutboxDispatcher>().Error(ex, "OutboxDispatcher loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
