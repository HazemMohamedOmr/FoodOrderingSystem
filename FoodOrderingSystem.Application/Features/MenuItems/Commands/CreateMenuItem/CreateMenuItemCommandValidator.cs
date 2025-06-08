using FluentValidation;

namespace FoodOrderingSystem.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
    {
        public CreateMenuItemCommandValidator()
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(v => v.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(v => v.RestaurantId)
                .NotEmpty().WithMessage("Restaurant ID is required.");
        }
    }
} 