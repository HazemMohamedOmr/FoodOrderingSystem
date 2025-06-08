using System;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.Application.Common.Models
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }
} 