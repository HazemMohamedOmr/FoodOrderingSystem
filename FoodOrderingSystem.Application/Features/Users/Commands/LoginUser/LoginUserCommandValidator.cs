using FluentValidation;

namespace FoodOrderingSystem.Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(v => v.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.");

            RuleFor(v => v.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
} 