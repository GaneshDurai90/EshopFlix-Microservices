using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CartService.Application.Messaging;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CartService.Infrastructure.HostedServices
{
    public sealed class OutboxDispatcherHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly TimeSpan _pollInterval;

        public OutboxDispatcherHostedService(IServiceProvider sp, IConfiguration cfg)
        {
            _sp = sp;
            var sec = cfg.GetValue<int?>("Outbox:PollSeconds") ?? 5;
            _pollInterval = TimeSpan.FromSeconds(sec);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<CartServiceDbContext>();
                    var broker = scope.ServiceProvider.GetRequiredService<IBrokerPublisher>();

                    // get a small batch and lock rows
                    var batch = await db.OutboxMessages
                        .Where(m => !m.Processed && m.LockedBy == null)
                        .OrderBy(m => m.OccurredOn)
                        .Take(50)
                        .ToListAsync(stoppingToken);

                    var workerId = Environment.MachineName;

                    foreach (var msg in batch)
                    {
                        msg.LockedBy = workerId;
                    }
                    await db.SaveChangesAsync(stoppingToken);

                    foreach (var msg in batch)
                    {
                        try
                        {
                            await broker.PublishAsync(msg.Type, msg.Destination ?? "default", msg.Content, stoppingToken);
                            msg.Processed = true;
                            msg.LockedBy = null;
                        }
                        catch (Exception ex)
                        {
                            // Unlock to retry later; log error
                            msg.LockedBy = null;
                            Log.Error(ex, "Failed to publish outbox message {MessageId} Type={Type}", msg.MessageId, msg.Type);
                        }
                    }
                    if (batch.Count > 0)
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Outbox dispatcher iteration failed");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }
}
