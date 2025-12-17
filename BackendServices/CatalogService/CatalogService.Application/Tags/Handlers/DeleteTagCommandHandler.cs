using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.Tags.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.Tags.Handlers
{
    public sealed class DeleteTagCommandHandler : ICommandHandler<DeleteTagCommand, bool>
    {
        private readonly ITagRepository _tagRepository;

        public DeleteTagCommandHandler(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<bool> Handle(DeleteTagCommand command, CancellationToken ct)
        {
            var tag = await _tagRepository.GetByIdAsync(command.TagId, ct);
            if (tag == null)
            {
                throw AppException.NotFound("tag", $"Tag {command.TagId} not found");
            }

            await _tagRepository.DeleteAsync(tag, ct);
            return true;
        }
    }
}
