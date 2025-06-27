using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderItems
{
    public class GetOrderItemsQuery : IRequest<Result<OrderItemsDto>>
    {
        public Guid OrderId { get; set; }
    }

    public class OrderItemsDto
    {
        public Guid OrderId { get; set; }
        public string RestaurantName { get; set; }
        public decimal DeliveryFee { get; set; }
        public string OrderStatus { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public string MenuItemDescription { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public decimal ItemTotal { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
    }

    public class GetOrderItemsQueryHandler : IRequestHandler<GetOrderItemsQuery, Result<OrderItemsDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetOrderItemsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<OrderItemsDto>> Handle(GetOrderItemsQuery request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            
            if (order == null)
            {
                return Result<OrderItemsDto>.Failure($"Order with ID {request.OrderId} not found.");
            }

            var orderItems = await _unitOfWork.OrderItems.FindAsync(
                oi => oi.OrderId == request.OrderId, cancellationToken);
                
            if (orderItems == null || !orderItems.Any())
            {
                return Result<OrderItemsDto>.Failure($"No items found for order with ID {request.OrderId}.");
            }
            
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
            
            var result = new OrderItemsDto
            {
                OrderId = order.Id,
                RestaurantName = restaurant?.Name ?? "Unknown Restaurant",
                DeliveryFee = restaurant?.DeliveryFee ?? 0m,
                OrderStatus = order.Status.ToString(),
                Items = new List<OrderItemDto>()
            };
            
            foreach (var item in orderItems)
            {
                var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                if (menuItem == null) continue;
                
                var user = await _unitOfWork.Users.GetByIdAsync(item.UserId, cancellationToken);
                
                var orderItemDto = new OrderItemDto
                {
                    Id = item.Id,
                    MenuItemId = item.MenuItemId,
                    MenuItemName = menuItem.Name,
                    MenuItemDescription = menuItem.Description,
                    Price = menuItem.Price,
                    Quantity = item.Quantity,
                    Note = item.Note,
                    ItemTotal = menuItem.Price * item.Quantity,
                    UserId = item.UserId,
                    UserName = user?.Name ?? "Unknown User"
                };
                
                result.Items.Add(orderItemDto);
            }
            
            return Result<OrderItemsDto>.Success(result);
        }
    }
} 