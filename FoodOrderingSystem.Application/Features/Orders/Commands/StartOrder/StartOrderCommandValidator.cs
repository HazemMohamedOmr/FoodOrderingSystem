using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.StartOrder
{
    public class StartOrderCommandValidator : AbstractValidator<StartOrderCommand>
    {
        public StartOrderCommandValidator()
        {
            RuleFor(v => v.RestaurantId)
                .NotEmpty().WithMessage("Restaurant ID is required.");

            RuleFor(v => v.ManagerId)
                .NotEmpty().WithMessage("Manager ID is required.");
        }
    }
} 