using System;
using FoodOrderingSystem.Domain.Common;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
    }
} 