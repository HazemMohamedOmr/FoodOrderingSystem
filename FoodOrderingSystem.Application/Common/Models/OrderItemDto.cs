using System;

namespace FoodOrderingSystem.Application.Common.Models
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserPhoneNumber { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public decimal MenuItemPrice { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DeliveryFeeShare { get; set; }
        public decimal Total { get; set; }
        public bool IsPaid { get; set; }
    }
} 