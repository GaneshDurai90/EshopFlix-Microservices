using CatalogService.API.Contracts.ProductRelationships;
using CatalogService.Application.DTO;
using CatalogService.Application.ProductRelationships.Commands;
using CatalogService.Application.ProductRelationships.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/relationships")]
    public class ProductRelationshipController : ControllerBase
    {
        private readonly IProductRelationshipAppService _relationshipService;

        public ProductRelationshipController(IProductRelationshipAppService relationshipService)
        {
            _relationshipService = relationshipService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductRelationshipDto>>> Get(int productId, [FromQuery] byte? relationshipType, CancellationToken ct)
        {
            var relationships = await _relationshipService.GetAsync(new GetProductRelationshipsQuery
            {
                ParentProductId = productId,
                RelationshipType = relationshipType
            }, ct);

            return Ok(relationships);
        }

        [HttpPost]
        public async Task<ActionResult<ProductRelationshipDto>> Add(int productId, [FromBody] ProductRelationshipRequest request, CancellationToken ct)
        {
            var relationship = await _relationshipService.AddAsync(new AddProductRelationshipCommand
            {
                ParentProductId = productId,
                RelatedProductId = request.RelatedProductId,
                RelationshipType = request.RelationshipType,
                SortOrder = request.SortOrder
            }, ct);

            return CreatedAtAction(nameof(Get), new { productId }, relationship);
        }

        [HttpPut("{relatedProductId:int}")]
        public async Task<ActionResult<ProductRelationshipDto>> Update(int productId, int relatedProductId, [FromBody] ProductRelationshipRequest request, CancellationToken ct)
        {
            var relationship = await _relationshipService.UpdateAsync(new UpdateProductRelationshipCommand
            {
                ParentProductId = productId,
                RelatedProductId = relatedProductId,
                RelationshipType = request.RelationshipType,
                SortOrder = request.SortOrder
            }, ct);

            return Ok(relationship);
        }

        [HttpDelete("{relatedProductId:int}")]
        public async Task<IActionResult> Delete(int productId, int relatedProductId, CancellationToken ct)
        {
            await _relationshipService.DeleteAsync(new DeleteProductRelationshipCommand
            {
                ParentProductId = productId,
                RelatedProductId = relatedProductId
            }, ct);

            return NoContent();
        }
    }
}
