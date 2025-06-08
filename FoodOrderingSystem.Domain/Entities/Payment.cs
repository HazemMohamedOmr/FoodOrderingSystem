using System;
using FoodOrderingSystem.Domain.Common;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public PaymentStatus Status { get; set; }
    }
} 