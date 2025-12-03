using System.Threading;
using System.Threading.Tasks;

namespace CartService.Application.Messaging
{
    public interface IBrokerPublisher
    {
        Task PublishAsync(string type, string destination, string content, CancellationToken ct = default);
    }
}
