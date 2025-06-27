using System;

namespace FoodOrderingSystem.Application.Common.Models
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string Expiration { get; set; }
        public UserDto User { get; set; }
    }
} 