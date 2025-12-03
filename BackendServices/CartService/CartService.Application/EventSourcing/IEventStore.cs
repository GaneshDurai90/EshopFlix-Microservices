using CartService.Domain.Events;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CartService.Application.EventSourcing
{
    public interface IEventStore
    {
        Task<IReadOnlyList<IDomainEvent>> LoadAsync(long cartId, CancellationToken ct = default);
        Task<int> AppendAsync(long cartId, IEnumerable<IDomainEvent> events, string? causedBy, CancellationToken ct = default);
    }
}
