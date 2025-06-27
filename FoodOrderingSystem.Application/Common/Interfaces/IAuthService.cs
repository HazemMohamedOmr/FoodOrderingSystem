using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;

namespace FoodOrderingSystem.Application.Common.Interfaces
{
    public interface IAuthService
    {
        Task<Result<string>> AuthenticateAsync(string phoneNumber, string password, CancellationToken cancellationToken = default);
        Task<Result<AuthResponseDto>> AuthenticateByEmailAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<Result<AuthResponseDto>> RegisterUserAsync(User user, string password, CancellationToken cancellationToken = default);
        Task<User> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<User> GetUserByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
        Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
        string CreatePasswordHash(string password);
    }
} 