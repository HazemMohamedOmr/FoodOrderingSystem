using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Users.Commands.UpdateUserRole
{
    public class UpdateUserRoleCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }
        public UserRole NewRole { get; set; }
    }

    public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateUserRoleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            // Get current user to confirm they are an admin
            var currentUserId = _currentUserService.UserId;
            
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Result.Failure("User is not authenticated.");
            }
            
            if (!Guid.TryParse(currentUserId, out Guid adminId))
            {
                return Result.Failure("Invalid user ID format.");
            }
            
            var admin = await _unitOfWork.Users.GetByIdAsync(adminId, cancellationToken);
            
            if (admin == null || admin.Role != UserRole.Admin)
            {
                return Result.Failure("Only administrators can update user roles.");
            }

            // Get the target user
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            
            if (user == null)
            {
                return Result.Failure($"User with ID {request.UserId} not found.");
            }
            
            // Cannot change role of another admin
            if (user.Role == UserRole.Admin && request.NewRole != UserRole.Admin)
            {
                return Result.Failure("Cannot change the role of an administrator.");
            }
            
            // Update the user role
            user.Role = request.NewRole;
            user.LastModifiedAt = DateTime.UtcNow;
            user.LastModifiedBy = admin.Name ?? admin.Email ?? adminId.ToString();
            
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Success();
        }
    }
} 