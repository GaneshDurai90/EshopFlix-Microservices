using CatalogService.API.Contracts.ProductImages;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductImages.Commands;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/images")]
    public class ProductImageController : ControllerBase
    {
        private readonly IProductImageAppService _imageService;

        public ProductImageController(IProductImageAppService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductImageDto>>> GetByProduct(int productId, CancellationToken ct)
        {
            var images = await _imageService.GetByProductAsync(productId, ct);
            return Ok(images);
        }

        [HttpGet("{imageId:int}")]
        public async Task<ActionResult<ProductImageDto>> GetById(int productId, int imageId, CancellationToken ct)
        {
            var image = await _imageService.GetByIdAsync(imageId, ct);
            if (image == null || image.ProductId != productId)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpPost]
        public async Task<ActionResult<ProductImageDto>> Create(int productId, [FromBody] CreateProductImageRequest request, CancellationToken ct)
        {
            var command = new CreateProductImageCommand
            {
                ProductId = productId,
                SkuId = request.SkuId,
                Url = request.Url,
                AltText = request.AltText,
                SortOrder = request.SortOrder,
                IsPrimary = request.IsPrimary
            };

            var image = await _imageService.CreateAsync(command, ct);
            return CreatedAtAction(nameof(GetById), new { productId, imageId = image.ProductImageId }, image);
        }

        [HttpPut("{imageId:int}")]
        public async Task<ActionResult<ProductImageDto>> Update(int productId, int imageId, [FromBody] UpdateProductImageRequest request, CancellationToken ct)
        {
            var command = new UpdateProductImageCommand
            {
                ProductImageId = imageId,
                Url = request.Url,
                AltText = request.AltText,
                SortOrder = request.SortOrder,
                IsPrimary = request.IsPrimary
            };

            var image = await _imageService.UpdateAsync(command, ct);
            if (image.ProductId != productId)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpDelete("{imageId:int}")]
        public async Task<IActionResult> Delete(int productId, int imageId, CancellationToken ct)
        {
            await _imageService.DeleteAsync(new DeleteProductImageCommand { ProductImageId = imageId }, ct);
            return NoContent();
        }

        [HttpPost("{imageId:int}/primary")]
        public async Task<ActionResult<ProductImageDto>> SetPrimary(int productId, int imageId, CancellationToken ct)
        {
            var image = await _imageService.SetPrimaryAsync(new SetPrimaryProductImageCommand
            {
                ProductId = productId,
                ProductImageId = imageId
            }, ct);

            return Ok(image);
        }
    }
}
