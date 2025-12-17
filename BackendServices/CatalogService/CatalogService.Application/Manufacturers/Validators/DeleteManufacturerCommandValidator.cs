using CatalogService.Application.Manufacturers.Commands;
using FluentValidation;

namespace CatalogService.Application.Manufacturers.Validators
{
    public sealed class DeleteManufacturerCommandValidator : AbstractValidator<DeleteManufacturerCommand>
    {
        public DeleteManufacturerCommandValidator()
        {
            RuleFor(x => x.ManufacturerId).GreaterThan(0);
        }
    }
}
