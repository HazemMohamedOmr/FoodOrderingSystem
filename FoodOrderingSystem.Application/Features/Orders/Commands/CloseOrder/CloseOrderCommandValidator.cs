using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.CloseOrder
{
    public class CloseOrderCommandValidator : AbstractValidator<CloseOrderCommand>
    {
        public CloseOrderCommandValidator()
        {
            RuleFor(v => v.OrderId)
                .NotEmpty().WithMessage("Order ID is required.");
        }
    }
} 