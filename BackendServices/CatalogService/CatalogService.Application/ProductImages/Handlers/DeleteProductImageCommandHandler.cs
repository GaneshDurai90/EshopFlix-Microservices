using System.Threading;
using System.Threading.Tasks;
using CatalogService.Application.CQRS;
using CatalogService.Application.Exceptions;
using CatalogService.Application.ProductImages.Commands;
using CatalogService.Application.Repositories;

namespace CatalogService.Application.ProductImages.Handlers
{
    public sealed class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, bool>
    {
        private readonly IProductImageRepository _imageRepository;

        public DeleteProductImageCommandHandler(IProductImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }

        public async Task<bool> Handle(DeleteProductImageCommand command, CancellationToken ct)
        {
            var image = await _imageRepository.GetByIdAsync(command.ProductImageId, ct);
            if (image == null)
            {
                throw AppException.NotFound("image", $"Image {command.ProductImageId} not found");
            }

            await _imageRepository.DeleteAsync(image, ct);
            return true;
        }
    }
}
