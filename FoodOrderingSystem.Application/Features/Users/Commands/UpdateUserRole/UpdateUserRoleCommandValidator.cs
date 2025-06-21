using FluentValidation;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.Application.Features.Users.Commands.UpdateUserRole
{
    public class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
    {
        public UpdateUserRoleCommandValidator()
        {
            RuleFor(v => v.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(v => v.NewRole)
                .IsInEnum().WithMessage("Invalid role.")
                .NotEqual(UserRole.Admin).WithMessage("Cannot set a user's role to Admin via this API.");
        }
    }
} 