using FluentValidation;

namespace CartService.API.Contracts
{
    public class AddItemRequestValidatorDTO : AbstractValidator<AddItemRequestDTO>
    {
        public AddItemRequestValidatorDTO()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.ItemId).GreaterThan(0);
            RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Quantity).GreaterThan(0);

            RuleFor(x => x.Sku).MaximumLength(64);
            RuleFor(x => x.ProductName).MaximumLength(256);
            RuleFor(x => x.TaxCategory).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.TaxCategory));
        }
    }

}
