using CatalogService.Application.DTO;
using CatalogService.Application.Products.Commands;
using CatalogService.Application.Products.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IProductAppService
    {
        Task<PagedResult<ProductListItemDto>> SearchAsync(SearchProductsQuery query, CancellationToken ct = default);
        Task<ProductDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<ProductDTO>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
        Task<ProductDetailDto> CreateAsync(CreateProductCommand command, CancellationToken ct = default);
        Task<ProductDetailDto> UpdateAsync(UpdateProductCommand command, CancellationToken ct = default);
        Task<ProductDetailDto> ChangeStatusAsync(ChangeProductStatusCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteProductCommand command, CancellationToken ct = default);
    }
}
