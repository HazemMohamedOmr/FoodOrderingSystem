using System;
using FoodOrderingSystem.Domain.Common;

namespace FoodOrderingSystem.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public Guid MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
    }
} 