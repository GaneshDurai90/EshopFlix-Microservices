using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Repositories;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Tags.Handlers
{
    public sealed class CreateTagCommandHandler : ICommandHandler<CreateTagCommand, TagDto>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public CreateTagCommandHandler(ITagRepository tagRepository, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<TagDto> Handle(CreateTagCommand command, CancellationToken ct)
        {
            var slug = command.Slug.Trim().ToLowerInvariant();
            var exists = await _tagRepository.ExistsBySlugAsync(slug, null, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["slug"] = new[] { "Tag slug already exists." }
                });
            }

            var tag = new Tag
            {
                Name = command.Name.Trim(),
                Slug = slug
            };

            await _tagRepository.AddAsync(tag, ct);
            return _mapper.Map<TagDto>(tag);
        }
    }
}
