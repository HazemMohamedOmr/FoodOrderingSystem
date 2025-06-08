using System;
using System.Collections.Generic;
using FoodOrderingSystem.Domain.Common;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }
        public Guid ManagerId { get; set; }
        public User Manager { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime OrderDate { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
} 