using System;
using System.Collections.Generic;

namespace FoodOrderingSystem.Application.Common.Models
{
    public class RestaurantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public decimal DeliveryFee { get; set; }
        public ICollection<MenuItemDto> MenuItems { get; set; }
    }
} 