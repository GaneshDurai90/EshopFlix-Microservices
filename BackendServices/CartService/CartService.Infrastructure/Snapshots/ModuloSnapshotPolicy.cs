using System;
using CartService.Application.Snapshots;
using Microsoft.Extensions.Configuration;

namespace CartService.Infrastructure.Snapshots
{
    public sealed class ModuloSnapshotPolicy : ISnapshotPolicy
    {
        private readonly int _interval;
        public ModuloSnapshotPolicy(IConfiguration cfg)
        {
            _interval = Math.Max(1, cfg.GetValue<int?>("EventSourcing:SnapshotInterval") ?? 100);
        }

        public bool ShouldSnapshot(int streamVersion) => streamVersion % _interval == 0;
    }
}
