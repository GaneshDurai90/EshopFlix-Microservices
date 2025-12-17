using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductReviews.Commands;
using CatalogService.Application.ProductReviews.Queries;

namespace CatalogService.Application.Services.Abstractions
{
    public interface IProductReviewAppService
    {
        Task<ProductReviewDto> CreateAsync(CreateProductReviewCommand command, CancellationToken ct = default);
        Task<ProductReviewDto> UpdateAsync(UpdateProductReviewCommand command, CancellationToken ct = default);
        Task DeleteAsync(DeleteProductReviewCommand command, CancellationToken ct = default);
        Task<PagedResult<ProductReviewListItemDto>> SearchAsync(SearchProductReviewsQuery query, CancellationToken ct = default);
    }
}
