using System.Collections.Generic;
using FoodOrderingSystem.Domain.Common;

namespace FoodOrderingSystem.Domain.Entities
{
    public class Restaurant : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public decimal DeliveryFee { get; set; }
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
} 