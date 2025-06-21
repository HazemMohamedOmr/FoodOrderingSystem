using AutoMapper;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Application.Features.Users.Queries.GetAllUsers;
using FoodOrderingSystem.Domain.Entities;

namespace FoodOrderingSystem.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
            
            CreateMap<User, UserListItemDto>();

            CreateMap<Restaurant, RestaurantDto>();

            CreateMap<MenuItem, MenuItemDto>();

            CreateMap<Order, OrderDto>();

            CreateMap<OrderItem, OrderItemDto>();
        }
    }
}