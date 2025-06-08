using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.UpdatePaymentStatus
{
    public class UpdatePaymentStatusCommandValidator : AbstractValidator<UpdatePaymentStatusCommand>
    {
        public UpdatePaymentStatusCommandValidator()
        {
            RuleFor(v => v.OrderId)
                .NotEmpty().WithMessage("Order ID is required.");

            RuleFor(v => v.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(v => v.ManagerId)
                .NotEmpty().WithMessage("Manager ID is required.");
        }
    }
}