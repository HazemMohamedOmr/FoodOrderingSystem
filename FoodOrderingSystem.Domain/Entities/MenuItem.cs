using System;
using FoodOrderingSystem.Domain.Common;

namespace FoodOrderingSystem.Domain.Entities
{
    public class MenuItem : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public Guid RestaurantId { get; set; }
        public Restaurant Restaurant { get; set; }
    }
} 