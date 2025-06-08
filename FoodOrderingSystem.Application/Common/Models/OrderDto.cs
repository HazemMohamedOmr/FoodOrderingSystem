using System;
using System.Collections.Generic;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.Application.Common.Models
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public Guid RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public Guid ManagerId { get; set; }
        public string ManagerName { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime OrderDate { get; set; }
        public ICollection<OrderItemDto> OrderItems { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
    }
}