using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.UpdateOrderItem
{
    public class UpdateOrderItemCommandValidator : AbstractValidator<UpdateOrderItemCommand>
    {
        public UpdateOrderItemCommandValidator()
        {
            RuleFor(v => v.OrderItemId)
                .NotEmpty().WithMessage("Order item ID is required.");
                
            RuleFor(v => v.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
                
            RuleFor(v => v.Note)
                .MaximumLength(500).WithMessage("Note must not exceed 500 characters.");
        }
    }
} 