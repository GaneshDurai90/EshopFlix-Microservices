using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Repositories
{
    public interface IProductRelationshipRepository
    {
        Task<IReadOnlyList<ProductRelationship>> GetByParentAsync(int parentProductId, byte? relationshipType, CancellationToken ct = default);
        Task AddAsync(ProductRelationship relationship, CancellationToken ct = default);
        Task UpdateAsync(ProductRelationship relationship, CancellationToken ct = default);
        Task DeleteAsync(int parentProductId, int relatedProductId, CancellationToken ct = default);
    }
}
