using System;
using CatalogService.API.Contracts.ProductReviews;
using CatalogService.API.Infrastructure.Idempotency;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductReviews.Commands;
using CatalogService.Application.ProductReviews.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/reviews")]
    public class ProductReviewController : ControllerBase
    {
        private readonly IProductReviewAppService _reviewService;
        private readonly IIdempotencyAppService _idempotency;
        private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);

        public ProductReviewController(IProductReviewAppService reviewService, IIdempotencyAppService idempotency)
        {
            _reviewService = reviewService;
            _idempotency = idempotency;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductReviewListItemDto>>> Search(int productId, [FromQuery] bool? isPublished, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var result = await _reviewService.SearchAsync(new SearchProductReviewsQuery
            {
                ProductId = productId,
                IsPublished = isPublished,
                Page = page,
                PageSize = pageSize
            }, ct);

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProductReviewDto>> Create(int productId, [FromBody] CreateProductReviewRequest request, CancellationToken ct)
        {
            var command = new CreateProductReviewCommand
            {
                ProductId = productId,
                UserId = request.UserId,
                Rating = request.Rating,
                Title = request.Title,
                Body = request.Body
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var review = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _reviewService.CreateAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            return CreatedAtAction(nameof(Search), new { productId }, review);
        }

        [HttpPut("{reviewId:int}")]
        public async Task<ActionResult<ProductReviewDto>> Update(int productId, int reviewId, [FromBody] UpdateProductReviewRequest request, CancellationToken ct)
        {
            var command = new UpdateProductReviewCommand
            {
                ReviewId = reviewId,
                Rating = request.Rating,
                Title = request.Title,
                Body = request.Body,
                IsPublished = request.IsPublished
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var review = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _reviewService.UpdateAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            if (review.ProductId != productId)
            {
                return NotFound();
            }

            return Ok(review);
        }

        [HttpDelete("{reviewId:int}")]
        public async Task<IActionResult> Delete(int productId, int reviewId, CancellationToken ct)
        {
            await _reviewService.DeleteAsync(new DeleteProductReviewCommand { ReviewId = reviewId }, ct);
            return NoContent();
        }
    }
}
