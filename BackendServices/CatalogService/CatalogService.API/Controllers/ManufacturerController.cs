using CatalogService.API.Contracts.Manufacturers;
using CatalogService.Application.DTO;
using CatalogService.Application.Manufacturers.Commands;
using CatalogService.Application.Manufacturers.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ManufacturerController : ControllerBase
    {
        private readonly IManufacturerAppService _manufacturerService;

        public ManufacturerController(IManufacturerAppService manufacturerService)
        {
            _manufacturerService = manufacturerService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ManufacturerListItemDto>>> Search([FromQuery] SearchManufacturersQuery query, CancellationToken ct)
        {
            query ??= new SearchManufacturersQuery();
            var result = await _manufacturerService.SearchAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ManufacturerDto>> GetById(int id, CancellationToken ct)
        {
            var manufacturer = await _manufacturerService.GetByIdAsync(id, ct);
            if (manufacturer == null)
            {
                return NotFound();
            }

            return Ok(manufacturer);
        }

        [HttpPost]
        public async Task<ActionResult<ManufacturerDto>> Create([FromBody] CreateManufacturerRequest request, CancellationToken ct)
        {
            var command = new CreateManufacturerCommand
            {
                Name = request.Name,
                ContactInfo = request.ContactInfo
            };

            var result = await _manufacturerService.CreateAsync(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.ManufacturerId }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ManufacturerDto>> Update(int id, [FromBody] UpdateManufacturerRequest request, CancellationToken ct)
        {
            var command = new UpdateManufacturerCommand
            {
                ManufacturerId = id,
                Name = request.Name,
                ContactInfo = request.ContactInfo
            };

            var result = await _manufacturerService.UpdateAsync(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _manufacturerService.DeleteAsync(new DeleteManufacturerCommand { ManufacturerId = id }, ct);
            return NoContent();
        }
    }
}
