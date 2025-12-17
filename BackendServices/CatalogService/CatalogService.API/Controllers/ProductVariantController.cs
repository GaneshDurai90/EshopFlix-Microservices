using CatalogService.API.Contracts.ProductVariants;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductVariants.Commands;
using CatalogService.Application.ProductVariants.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/variants")]
    public class ProductVariantController : ControllerBase
    {
        private readonly IProductVariantAppService _variantService;

        public ProductVariantController(IProductVariantAppService variantService)
        {
            _variantService = variantService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductVariantListItemDto>>> GetByProduct(int productId, CancellationToken ct)
        {
            var variants = await _variantService.GetByProductAsync(productId, ct);
            return Ok(variants);
        }

        [HttpGet("{skuId:int}")]
        public async Task<ActionResult<ProductVariantDto>> GetById(int productId, int skuId, CancellationToken ct)
        {
            var variant = await _variantService.GetByIdAsync(skuId, ct);
            if (variant == null || variant.ProductId != productId)
            {
                return NotFound();
            }

            return Ok(variant);
        }

        [HttpPost]
        public async Task<ActionResult<ProductVariantDto>> Create(int productId, [FromBody] CreateProductVariantRequest request, CancellationToken ct)
        {
            var command = new CreateProductVariantCommand
            {
                ProductId = productId,
                Sku = request.Sku,
                Barcode = request.Barcode,
                Attributes = request.Attributes,
                UnitPrice = request.UnitPrice,
                Currency = request.Currency,
                CompareAtPrice = request.CompareAtPrice,
                CostPrice = request.CostPrice,
                IsDefault = request.IsDefault
            };

            var variant = await _variantService.CreateAsync(command, ct);
            return CreatedAtAction(nameof(GetById), new { productId, skuId = variant.SkuId }, variant);
        }

        [HttpPut("{skuId:int}")]
        public async Task<ActionResult<ProductVariantDto>> Update(int productId, int skuId, [FromBody] UpdateProductVariantRequest request, CancellationToken ct)
        {
            var command = new UpdateProductVariantCommand
            {
                SkuId = skuId,
                Sku = request.Sku,
                Barcode = request.Barcode,
                Attributes = request.Attributes,
                UnitPrice = request.UnitPrice,
                Currency = request.Currency,
                CompareAtPrice = request.CompareAtPrice,
                CostPrice = request.CostPrice,
                IsDefault = request.IsDefault
            };

            var variant = await _variantService.UpdateAsync(command, ct);
            if (variant.ProductId != productId)
            {
                return NotFound();
            }

            return Ok(variant);
        }

        [HttpDelete("{skuId:int}")]
        public async Task<IActionResult> Delete(int productId, int skuId, CancellationToken ct)
        {
            await _variantService.DeleteAsync(new DeleteProductVariantCommand { SkuId = skuId }, ct);
            return NoContent();
        }

        [HttpPost("{skuId:int}/default")]
        public async Task<ActionResult<ProductVariantDto>> SetDefault(int productId, int skuId, CancellationToken ct)
        {
            var command = new SetDefaultProductVariantCommand
            {
                ProductId = productId,
                SkuId = skuId
            };

            var variant = await _variantService.SetDefaultAsync(command, ct);
            return Ok(variant);
        }
    }
}
