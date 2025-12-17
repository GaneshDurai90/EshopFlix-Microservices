using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CatalogService.Application.CQRS;
using CatalogService.Application.DTO;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Tags.Handlers
{
    public sealed class UpdateTagCommandHandler : ICommandHandler<UpdateTagCommand, TagDto>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public UpdateTagCommandHandler(ITagRepository tagRepository, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<TagDto> Handle(UpdateTagCommand command, CancellationToken ct)
        {
            var tag = await _tagRepository.GetByIdAsync(command.TagId, ct);
            if (tag == null)
            {
                throw AppException.NotFound("tag", $"Tag {command.TagId} not found");
            }

            var slug = command.Slug.Trim().ToLowerInvariant();
            var exists = await _tagRepository.ExistsBySlugAsync(slug, command.TagId, ct);
            if (exists)
            {
                throw AppException.Validation(new Dictionary<string, string[]>
                {
                    ["slug"] = new[] { "Tag slug already exists." }
                });
            }

            tag.Name = command.Name.Trim();
            tag.Slug = slug;

            await _tagRepository.UpdateAsync(tag, ct);
            return _mapper.Map<TagDto>(tag);
        }
    }
}
