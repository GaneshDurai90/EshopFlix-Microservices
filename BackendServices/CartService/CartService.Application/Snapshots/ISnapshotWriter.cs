using System;
using System.Threading;
using System.Threading.Tasks;

namespace CartService.Application.Snapshots
{
    public interface ISnapshotWriter
    {
        Task WriteAsync(long cartId, CancellationToken ct = default);
    }
}
