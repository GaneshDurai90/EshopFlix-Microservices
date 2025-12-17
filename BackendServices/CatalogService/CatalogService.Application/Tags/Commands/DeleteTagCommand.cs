using CatalogService.Application.CQRS;

namespace CatalogService.Application.Tags.Commands
{
    public sealed class DeleteTagCommand : ICommand<bool>
    {
        public int TagId { get; init; }
    }
}
