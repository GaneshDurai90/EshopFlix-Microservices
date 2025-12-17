using CatalogService.API.Contracts.Tags;
using CatalogService.Application.DTO;
using CatalogService.Application.Services.Abstractions;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Tags.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/tags")]
    public class TagController : ControllerBase
    {
        private readonly ITagAppService _tagService;

        public TagController(ITagAppService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TagListItemDto>>> Search([FromQuery] SearchTagsQuery query, CancellationToken ct)
        {
            query ??= new SearchTagsQuery();
            var result = await _tagService.SearchAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken ct)
        {
            var tags = await _tagService.GetAllAsync(ct);
            return Ok(tags);
        }

        [HttpPost]
        public async Task<ActionResult<TagDto>> Create([FromBody] CreateTagRequest request, CancellationToken ct)
        {
            var command = new CreateTagCommand
            {
                Name = request.Name,
                Slug = request.Slug
            };

            var tag = await _tagService.CreateAsync(command, ct);
            return CreatedAtAction(nameof(GetAll), null, tag);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TagDto>> Update(int id, [FromBody] UpdateTagRequest request, CancellationToken ct)
        {
            var command = new UpdateTagCommand
            {
                TagId = id,
                Name = request.Name,
                Slug = request.Slug
            };

            var tag = await _tagService.UpdateAsync(command, ct);
            return Ok(tag);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _tagService.DeleteAsync(new DeleteTagCommand { TagId = id }, ct);
            return NoContent();
        }

        [HttpPost("products/{productId:int}")]
        public async Task<ActionResult<ProductDetailDto>> AssignToProduct(int productId, [FromBody] AssignTagsRequest request, CancellationToken ct)
        {
            var command = new AssignTagsToProductCommand
            {
                ProductId = productId,
                TagIds = request.TagIds
            };

            var product = await _tagService.AssignToProductAsync(command, ct);
            return Ok(product);
        }
    }
}
