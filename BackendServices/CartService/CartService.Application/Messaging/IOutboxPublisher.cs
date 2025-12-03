using System.Threading;
using System.Threading.Tasks;

namespace CartService.Application.Messaging
{
    public interface IOutboxPublisher
    {
        Task EnqueueAsync(string type, object payload, string? destination = null, CancellationToken ct = default);
    }
}
