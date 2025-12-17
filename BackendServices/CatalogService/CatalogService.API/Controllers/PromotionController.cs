using System;
using CatalogService.API.Contracts.Promotions;
using CatalogService.API.Infrastructure.Idempotency;
using CatalogService.Application.DTO;
using CatalogService.Application.Promotions.Commands;
using CatalogService.Application.Promotions.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionAppService _promotionService;
        private readonly IIdempotencyAppService _idempotency;
        private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(30);

        public PromotionController(IPromotionAppService promotionService, IIdempotencyAppService idempotency)
        {
            _promotionService = promotionService;
            _idempotency = idempotency;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<PromotionListItemDto>>> Search([FromQuery] SearchPromotionsQuery query, CancellationToken ct)
        {
            query ??= new SearchPromotionsQuery();
            var result = await _promotionService.SearchAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PromotionDto>> GetById(int id, CancellationToken ct)
        {
            var promotion = await _promotionService.GetByIdAsync(id, ct);
            if (promotion == null)
            {
                return NotFound();
            }

            return Ok(promotion);
        }

        [HttpPost]
        public async Task<ActionResult<PromotionDto>> Create([FromBody] CreatePromotionRequest request, CancellationToken ct)
        {
            var command = new CreatePromotionCommand
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                AppliesToAllProducts = request.AppliesToAllProducts,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                ProductIds = request.ProductIds
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var promotion = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _promotionService.CreateAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            return CreatedAtAction(nameof(GetById), new { id = promotion.PromotionId }, promotion);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PromotionDto>> Update(int id, [FromBody] UpdatePromotionRequest request, CancellationToken ct)
        {
            var command = new UpdatePromotionCommand
            {
                PromotionId = id,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                AppliesToAllProducts = request.AppliesToAllProducts,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                ProductIds = request.ProductIds
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var promotion = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _promotionService.UpdateAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            return Ok(promotion);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _promotionService.DeleteAsync(new DeletePromotionCommand { PromotionId = id }, ct);
            return NoContent();
        }
    }
}
