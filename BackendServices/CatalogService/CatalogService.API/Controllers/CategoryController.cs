using CatalogService.API.Contracts.Categories;
using CatalogService.Application.Categories.Commands;
using CatalogService.Application.DTO;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryAppService _categoryService;

        public CategoryController(ICategoryAppService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryDto>> GetById(int id, CancellationToken ct)
        {
            var category = await _categoryService.GetByIdAsync(id, ct);
            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<CategoryNodeDto>>> GetTree([FromQuery] bool includeInactive, CancellationToken ct)
        {
            var tree = await _categoryService.GetTreeAsync(includeInactive, ct);
            return Ok(tree);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
        {
            var command = new CreateCategoryCommand
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                ParentCategoryId = request.ParentCategoryId,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive
            };

            var result = await _categoryService.CreateAsync(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.CategoryId }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        {
            var command = new UpdateCategoryCommand
            {
                CategoryId = id,
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                ParentCategoryId = request.ParentCategoryId,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive
            };

            var result = await _categoryService.UpdateAsync(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _categoryService.DeleteAsync(new DeleteCategoryCommand { CategoryId = id }, ct);
            return NoContent();
        }
    }
}
