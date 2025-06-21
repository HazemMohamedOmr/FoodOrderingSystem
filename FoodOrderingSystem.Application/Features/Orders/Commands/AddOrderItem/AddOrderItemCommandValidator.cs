using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.AddOrderItem
{
    public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemCommandValidator()
        {
            RuleFor(v => v.OrderId)
                .NotEmpty().WithMessage("Order ID is required.");

            RuleFor(v => v.MenuItemId)
                .NotEmpty().WithMessage("Menu item ID is required.");

            RuleFor(v => v.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            RuleFor(v => v.Note)
                .MaximumLength(500).WithMessage("Note must not exceed 500 characters.");
        }
    }
} 