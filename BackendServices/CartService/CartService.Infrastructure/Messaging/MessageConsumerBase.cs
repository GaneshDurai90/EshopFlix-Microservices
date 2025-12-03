using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CartService.Infrastructure.Messaging
{
    public abstract class MessageConsumerBase<T>
    {
        protected readonly CartServiceDbContext _db;
        protected readonly ILogger _log;

        protected MessageConsumerBase(CartServiceDbContext db)
        {
            _db = db;
            _log = Log.ForContext(GetType());
        }

        /// <summary>
        /// Process a message with inbox idempotency.
        /// Returns true if processed successfully or already processed (idempotent).
        /// </summary>
        public async Task<bool> ProcessWithInboxAsync(string messageId, string messageType, string content, Func<Task> process, string consumer = null, CancellationToken ct = default)
        {
            try
            {
                var inbox = new Domain.Entities.InboxMessage
                {
                    MessageId = messageId,
                    MessageType = messageType,
                    Content = content,
                    Consumer = consumer
                };
                _db.InboxMessages.Add(inbox);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
            {
                _log.Information("Duplicate message detected (id={MessageId}), skipping", messageId);
                return true;
            }

            // Now perform the actual processing
            try
            {
                await process();
                var rec = await _db.InboxMessages.FirstOrDefaultAsync(i => i.MessageId == messageId, ct);
                if (rec != null)
                {
                    rec.ProcessedOn = DateTime.UtcNow;
                    rec.Status = "Processed";
                    _db.InboxMessages.Update(rec);
                    await _db.SaveChangesAsync(ct);
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Processing failed for message {MessageId}", messageId);
                // leave inbox as Received so re-delivery will retry
                return false;
            }
        }

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            // SQL Server: 2627 or 2601 error codes; other DBs have different messages
            var inner = ex.InnerException?.Message ?? ex.Message;
            return inner.Contains("Cannot insert duplicate key") || inner.Contains("Violation of UNIQUE KEY constraint");
        }
    }
}
