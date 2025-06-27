using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Users.Commands.LoginUser
{
    public class LoginUserCommand : IRequest<Result<AuthResponseDto>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthResponseDto>>
    {
        private readonly IAuthService _authService;

        public LoginUserCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<Result<AuthResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            return await _authService.AuthenticateByEmailAsync(request.Email, request.Password, cancellationToken);
        }
    }
} 