using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FoodOrderingSystem.Infrastructure.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<Result<string>> AuthenticateAsync(string phoneNumber, string password, CancellationToken cancellationToken = default)
        {
            var user = await GetUserByPhoneNumberAsync(phoneNumber, cancellationToken);

            if (user == null)
            {
                return Result<string>.Failure("Invalid phone number or password.");
            }

            if (!VerifyPasswordHash(password, user.PasswordHash))
            {
                return Result<string>.Failure("Invalid phone number or password.");
            }

            var token = GenerateJwtToken(user);
            return Result<string>.Success(token);
        }

        public async Task<Result<User>> RegisterUserAsync(User user, string password, CancellationToken cancellationToken = default)
        {
            if (await GetUserByPhoneNumberAsync(user.PhoneNumber, cancellationToken) != null)
            {
                return Result<User>.Failure("Phone number is already registered.");
            }

            user.PasswordHash = CreatePasswordHash(password);

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<User>.Success(user);
        }

        public async Task<User> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (Guid.TryParse(userId, out Guid id))
            {
                return await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
            }
            return null;
        }

        public async Task<User> GetUserByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
            return users.Count > 0 ? users[0] : null;
        }

        public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string CreatePasswordHash(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            var result = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);

            return Convert.ToBase64String(result);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);
            
            int saltSize = 128 / 8; // HMACSHA512 key size
            byte[] salt = new byte[saltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, saltSize);

            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != hashBytes[i + saltSize])
                    return false;
            }

            return true;
        }
    }
} 