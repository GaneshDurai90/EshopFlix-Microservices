using System;
using CatalogService.Application.Manufacturers.Commands;
using FluentValidation;

namespace CatalogService.Application.Manufacturers.Validators
{
    public sealed class CreateManufacturerCommandValidator : AbstractValidator<CreateManufacturerCommand>
    {
        public CreateManufacturerCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.ContactInfo)
                .MaximumLength(1000)
                .When(x => x.ContactInfo != null);
        }
    }
}
