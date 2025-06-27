using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 10000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        public AuthService(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _mapper = mapper;
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

        public async Task<Result<AuthResponseDto>> AuthenticateByEmailAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await GetUserByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password.");
            }

            if (!VerifyPasswordHash(password, user.PasswordHash))
            {
                return Result<AuthResponseDto>.Failure("Invalid email or password.");
            }

            // Generate token with expiration
            DateTime expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
            var token = GenerateJwtToken(user, expiration);
            
            // Create response
            var response = new AuthResponseDto
            {
                Token = token,
                Expiration = expiration.ToString("o"), // ISO 8601 format
                User = _mapper.Map<UserDto>(user)
            };
            
            return Result<AuthResponseDto>.Success(response);
        }

        public async Task<Result<AuthResponseDto>> RegisterUserAsync(User user, string password, CancellationToken cancellationToken = default)
        {
            // Set password hash
            user.PasswordHash = CreatePasswordHash(password);

            // Save the user
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Generate token with expiration
            DateTime expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
            var token = GenerateJwtToken(user, expiration);
            
            // Create response
            var response = new AuthResponseDto
            {
                Token = token,
                Expiration = expiration.ToString("o"), // ISO 8601 format
                User = _mapper.Map<UserDto>(user)
            };

            return Result<AuthResponseDto>.Success(response);
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

        public async Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Email == email, cancellationToken);
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
            return GenerateJwtToken(user, DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));
        }

        private string GenerateJwtToken(User user, DateTime expiration)
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
                Expires = expiration,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string CreatePasswordHash(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with PBKDF2
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: salt,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithm,
                outputLength: KeySize);

            // Combine salt and hash
            byte[] hashBytes = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);

            // Convert to base64 for storage
            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            try
            {
                // Convert from base64 string
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                // Extract salt (first SaltSize bytes)
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Hash the input password
                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    password: password,
                    salt: salt,
                    iterations: Iterations,
                    hashAlgorithm: HashAlgorithm,
                    outputLength: KeySize);

                // Compare hash values
                for (int i = 0; i < KeySize; i++)
                {
                    if (hashBytes[i + SaltSize] != computedHash[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 