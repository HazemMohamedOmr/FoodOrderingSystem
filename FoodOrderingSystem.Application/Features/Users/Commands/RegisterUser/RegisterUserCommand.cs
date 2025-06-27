using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using FoodOrderingSystem.Domain.Enums;
using MediatR;
using System;

namespace FoodOrderingSystem.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<Result<AuthResponseDto>>
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public RegisterUserCommandHandler(IAuthService authService, IMapper mapper)
        {
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<Result<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Check for existing phone number
            var existingUserByPhone = await _authService.GetUserByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
            if (existingUserByPhone != null)
            {
                return Result<AuthResponseDto>.Failure("Phone number is already registered.");
            }

            // Check for existing email
            var existingUserByEmail = await _authService.GetUserByEmailAsync(request.Email, cancellationToken);
            if (existingUserByEmail != null)
            {
                return Result<AuthResponseDto>.Failure("Email is already registered.");
            }

            var user = new User
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Role = UserRole.EndUser,
                CreatedBy = "System",
                LastModifiedBy = "System",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _authService.RegisterUserAsync(user, request.Password, cancellationToken);

            if (!result.Succeeded)
            {
                return Result<AuthResponseDto>.Failure(result.Errors);
            }

            return result;
        }
    }
} 