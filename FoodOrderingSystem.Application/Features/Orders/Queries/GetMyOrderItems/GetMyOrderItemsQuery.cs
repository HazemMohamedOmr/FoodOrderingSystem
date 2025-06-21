using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetMyOrderItems
{
    public class GetMyOrderItemsQuery : IRequest<Result<MyOrderItemsDto>>
    {
        public Guid OrderId { get; set; }
    }
    
    public class MyOrderItemsDto
    {
        public Guid OrderId { get; set; }
        public string RestaurantName { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public List<MyOrderItemDto> Items { get; set; } = new List<MyOrderItemDto>();
        public decimal Subtotal { get; set; }
        public decimal DeliveryFeeShare { get; set; }
        public decimal Total { get; set; }
    }
    
    public class MyOrderItemDto
    {
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public string MenuItemDescription { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public decimal ItemTotal { get; set; }
        public bool CanBeDeleted { get; set; }
    }
    
    public class GetMyOrderItemsQueryHandler : IRequestHandler<GetMyOrderItemsQuery, Result<MyOrderItemsDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        
        public GetMyOrderItemsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }
        
        public async Task<Result<MyOrderItemsDto>> Handle(GetMyOrderItemsQuery request, CancellationToken cancellationToken)
        {
            // Get current user ID
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Result<MyOrderItemsDto>.Failure("User ID not found or invalid.");
            }
            
            // Get the order
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result<MyOrderItemsDto>.Failure($"Order with ID {request.OrderId} not found.");
            }
            
            // Create response DTO
            var response = new MyOrderItemsDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status
            };
            
            // Get restaurant info
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
            if (restaurant != null)
            {
                response.RestaurantName = restaurant.Name;
            }
            
            // Get user's order items
            var orderItems = await _unitOfWork.OrderItems.FindAsync(
                oi => oi.OrderId == order.Id && oi.UserId == userId,
                cancellationToken);
            
            decimal subtotal = 0;
            
            foreach (var item in orderItems)
            {
                var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                if (menuItem == null) continue;
                
                decimal itemTotal = menuItem.Price * item.Quantity;
                subtotal += itemTotal;
                
                var orderItemDto = new MyOrderItemDto
                {
                    Id = item.Id,
                    MenuItemId = menuItem.Id,
                    MenuItemName = menuItem.Name,
                    MenuItemDescription = menuItem.Description,
                    Price = menuItem.Price,
                    Quantity = item.Quantity,
                    Note = item.Note,
                    ItemTotal = itemTotal,
                    // Items can be deleted only if order is still active/open
                    CanBeDeleted = order.Status == OrderStatus.Open
                };
                
                response.Items.Add(orderItemDto);
            }
            
            response.Subtotal = subtotal;
            
            // Calculate delivery fee share
            int participantCount = (await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken))
                .Select(oi => oi.UserId)
                .Distinct()
                .Count();
                
            if (participantCount > 0 && restaurant != null)
            {
                response.DeliveryFeeShare = restaurant.DeliveryFee / participantCount;
            }
            
            response.Total = response.Subtotal + response.DeliveryFeeShare;
            
            return Result<MyOrderItemsDto>.Success(response);
        }
    }
} 