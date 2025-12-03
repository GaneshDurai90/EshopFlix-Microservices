using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Application.Snapshots
{
    public interface ISnapshotPolicy
    {
        bool ShouldSnapshot(int streamVersion);
    }
}
