using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PriceHistoryEntity = CatalogService.Domain.Entities.PriceHistory;

namespace CatalogService.Application.Repositories
{
    public interface IPriceHistoryRepository
    {
        Task RecordAsync(PriceHistoryEntity entry, CancellationToken ct = default);
        Task<(IReadOnlyList<PriceHistoryEntity> Items, int TotalCount)> GetHistoryAsync(int? productId, int? skuId, int page, int pageSize, CancellationToken ct = default);
    }
}
