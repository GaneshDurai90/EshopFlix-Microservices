using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductRelationships.Commands;
using CatalogService.Application.ProductRelationships.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IProductRelationshipAppService
    {
        Task<ProductRelationshipDto> AddAsync(AddProductRelationshipCommand command, CancellationToken ct = default);
        Task<ProductRelationshipDto> UpdateAsync(UpdateProductRelationshipCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteProductRelationshipCommand command, CancellationToken ct = default);
        Task<IReadOnlyList<ProductRelationshipDto>> GetAsync(GetProductRelationshipsQuery query, CancellationToken ct = default);
    }
}
