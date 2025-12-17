using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.PriceHistory.Commands;
using CatalogService.Application.Repositories;
using PriceHistoryEntity = CatalogService.Domain.Entities.PriceHistory;

namespace CatalogService.Application.PriceHistory.Handlers
{
    public sealed class RecordPriceChangeCommandHandler : ICommandHandler<RecordPriceChangeCommand, PriceHistoryEntryDto>
    {
        private readonly IPriceHistoryRepository _repository;
        private readonly IMapper _mapper;

        public RecordPriceChangeCommandHandler(IPriceHistoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PriceHistoryEntryDto> Handle(RecordPriceChangeCommand command, CancellationToken ct)
        {
            if (!command.ProductId.HasValue && !command.SkuId.HasValue)
            {
                throw new ArgumentException("Either ProductId or SkuId must be provided.");
            }

            var entry = new PriceHistoryEntity
            {
                ProductId = command.ProductId,
                SkuId = command.SkuId,
                OldPrice = command.OldPrice,
                NewPrice = command.NewPrice,
                Currency = command.Currency ?? "USD",
                ChangedBy = command.ChangedBy ?? string.Empty,
                ChangedDate = DateTime.UtcNow
            };

            await _repository.RecordAsync(entry, ct);
            return _mapper.Map<PriceHistoryEntryDto>(entry);
        }
    }
}
