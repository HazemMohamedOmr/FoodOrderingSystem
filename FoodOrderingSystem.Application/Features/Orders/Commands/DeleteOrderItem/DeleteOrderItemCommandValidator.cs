using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.DeleteOrderItem
{
    public class DeleteOrderItemCommandValidator : AbstractValidator<DeleteOrderItemCommand>
    {
        public DeleteOrderItemCommandValidator()
        {
            RuleFor(v => v.OrderItemId)
                .NotEmpty().WithMessage("Order item ID is required.");
        }
    }
} 