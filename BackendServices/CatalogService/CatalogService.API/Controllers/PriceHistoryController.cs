using System;
using CatalogService.API.Contracts.PriceHistory;
using CatalogService.API.Infrastructure.Idempotency;
using CatalogService.Application.DTO;
using CatalogService.Application.PriceHistory.Commands;
using CatalogService.Application.PriceHistory.Queries;
using CatalogService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PriceHistoryController : ControllerBase
    {
        private readonly IPriceHistoryAppService _priceHistoryService;
        private readonly IIdempotencyAppService _idempotency;
        private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(15);

        public PriceHistoryController(IPriceHistoryAppService priceHistoryService, IIdempotencyAppService idempotency)
        {
            _priceHistoryService = priceHistoryService;
            _idempotency = idempotency;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<PriceHistoryEntryDto>>> GetHistory([FromQuery] GetPriceHistoryQuery query, CancellationToken ct)
        {
            var result = await _priceHistoryService.GetHistoryAsync(query, ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<PriceHistoryEntryDto>> Record([FromBody] RecordPriceChangeRequest request, CancellationToken ct)
        {
            var command = new RecordPriceChangeCommand
            {
                ProductId = request.ProductId,
                SkuId = request.SkuId,
                OldPrice = request.OldPrice,
                NewPrice = request.NewPrice,
                Currency = request.Currency,
                ChangedBy = request.ChangedBy
            };

            var (key, hash) = IdempotencyKeyResolver.Resolve(Request, request);
            var entry = await _idempotency.ExecuteAsync(
                key,
                userId: null,
                _ => _priceHistoryService.RecordAsync(command, ct),
                ttl: IdempotencyTtl,
                requestHash: hash,
                ct: ct);
            return Ok(entry);
        }
    }
}
