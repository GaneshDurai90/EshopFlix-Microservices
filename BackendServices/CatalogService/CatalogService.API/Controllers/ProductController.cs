using System;
using CatalogService.API.Contracts.Products;
using CatalogService.API.Infrastructure.Idempotency;
using CatalogService.Application.DTO;
using CatalogService.Application.Products.Commands;
using CatalogService.Application.Products.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductAppService _productService;
        private readonly IIdempotencyAppService _idempotency;
        private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);

        public ProductController(IProductAppService productService, IIdempotencyAppService idempotency)
        {
            _productService = productService;
            _idempotency = idempotency;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductListItemDto>>> Search([FromQuery] SearchProductsQuery query, CancellationToken ct)
        {
            query ??= new SearchProductsQuery();
            var result = await _productService.SearchAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDetailDto>> GetById(int id, CancellationToken ct)
        {
            var product = await _productService.GetByIdAsync(id, ct);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDetailDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
        {
            var command = new CreateProductCommand
            {
                Name = request.Name,
                Slug = request.Slug,
                ShortDescription = request.ShortDescription,
                LongDescription = request.LongDescription,
                BrandId = request.BrandId,
                ManufacturerId = request.ManufacturerId,
                CategoryId = request.CategoryId,
                IsSearchable = request.IsSearchable,
                Weight = request.Weight,
                Dimensions = request.Dimensions,
                PrimaryImageUrl = request.PrimaryImageUrl,
                SeoTitle = request.SeoTitle,
                SeoDescription = request.SeoDescription,
                SeoKeywords = request.SeoKeywords,
                Status = (byte)request.Status
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var result = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _productService.CreateAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            return CreatedAtAction(nameof(GetById), new { id = result.ProductId }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ProductDetailDto>> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        {
            var command = new UpdateProductCommand
            {
                ProductId = id,
                Name = request.Name,
                Slug = request.Slug,
                ShortDescription = request.ShortDescription,
                LongDescription = request.LongDescription,
                BrandId = request.BrandId,
                ManufacturerId = request.ManufacturerId,
                CategoryId = request.CategoryId,
                IsSearchable = request.IsSearchable,
                Weight = request.Weight,
                Dimensions = request.Dimensions,
                PrimaryImageUrl = request.PrimaryImageUrl,
                SeoTitle = request.SeoTitle,
                SeoDescription = request.SeoDescription,
                SeoKeywords = request.SeoKeywords,
                Status = (byte)request.Status
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var result = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _productService.UpdateAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            return Ok(result);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<ProductDetailDto>> ChangeStatus(int id, [FromBody] ChangeProductStatusRequest request, CancellationToken ct)
        {
            var command = new ChangeProductStatusCommand
            {
                ProductId = id,
                Status = request.Status
            };

            var result = await _productService.ChangeStatusAsync(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _productService.DeleteAsync(new DeleteProductCommand { ProductId = id }, ct);
            return NoContent();
        }
    }
}
