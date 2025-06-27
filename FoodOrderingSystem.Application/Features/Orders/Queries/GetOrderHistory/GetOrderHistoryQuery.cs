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

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderHistory
{
    public class GetOrderHistoryQuery : IRequest<Result<List<OrderHistoryDto>>>
    {
        public Guid? UserId { get; set; }
        public Guid? RestaurantId { get; set; }
        public bool IncludeOtherParticipants { get; set; } = false;
        public bool ShowAllOrders { get; set; } = false;
    }

    public class OrderHistoryDto
    {
        public Guid Id { get; set; }
        public string RestaurantName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ClosedAt { get; set; }
        public OrderStatus Status { get; set; }
        public string ManagerName { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DeliveryFeeShare { get; set; }
        public List<UserOrderItemDto> UserItems { get; set; } = new List<UserOrderItemDto>();
        public PaymentStatus UserPaymentStatus { get; set; }
        public decimal UserTotal { get; set; }
    }

    public class UserOrderItemDto
    {
        public Guid Id { get; set; }
        public string MenuItemName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public decimal Total { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
    }

    public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, Result<List<OrderHistoryDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetOrderHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<OrderHistoryDto>>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
        {
            // Get all orders
            var orders = await _unitOfWork.Orders.GetAllAsync(cancellationToken);
            var filteredOrders = orders.AsQueryable();

            // Determine if we should filter by user
            bool shouldFilterByUser = !request.ShowAllOrders;
            Guid? filterUserId = request.UserId;

            // If we should filter by user but no specific user is provided, use the current user
            if (shouldFilterByUser && !filterUserId.HasValue && !string.IsNullOrEmpty(_currentUserService.UserId))
            {
                if (Guid.TryParse(_currentUserService.UserId, out Guid currentUserId))
                {
                    filterUserId = currentUserId;
                }
            }

            // Filter by restaurant if provided
            if (request.RestaurantId.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.RestaurantId == request.RestaurantId);
            }

            // Only filter by user if we should filter and have a user ID
            if (shouldFilterByUser && filterUserId.HasValue)
            {
                // Find orders where the user participated
                var userOrderItems = await _unitOfWork.OrderItems.FindAsync(
                    oi => oi.UserId == filterUserId, cancellationToken);

                var userOrderIds = userOrderItems.Select(oi => oi.OrderId).Distinct();
                filteredOrders = filteredOrders.Where(o => userOrderIds.Contains(o.Id));
            }

            // Order by date descending
            var orderedOrders = filteredOrders.OrderByDescending(o => o.CreatedAt).ToList();

            // Create response DTOs
            var result = new List<OrderHistoryDto>();

            foreach (var order in orderedOrders)
            {
                var orderHistory = new OrderHistoryDto
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    ClosedAt = order.ClosedAt,
                    Status = order.Status
                };

                // Get restaurant information
                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant != null)
                {
                    orderHistory.RestaurantName = restaurant.Name;
                    orderHistory.DeliveryFee = restaurant.DeliveryFee;
                }

                // Get manager information
                var manager = await _unitOfWork.Users.GetByIdAsync(order.ManagerId, cancellationToken);
                if (manager != null)
                {
                    orderHistory.ManagerName = manager.Name;
                }

                // Determine which order items to include
                IEnumerable<Domain.Entities.OrderItem> orderItems;
                
                if (shouldFilterByUser && filterUserId.HasValue && !request.IncludeOtherParticipants)
                {
                    // If filtering by user and don't want other participants, only get that user's items
                    orderItems = await _unitOfWork.OrderItems.FindAsync(
                        oi => oi.OrderId == order.Id && oi.UserId == filterUserId, cancellationToken);
                }
                else
                {
                    // Otherwise get all items for the order
                    orderItems = await _unitOfWork.OrderItems.FindAsync(
                        oi => oi.OrderId == order.Id, cancellationToken);
                }

                // Calculate delivery fee share
                int participantCount = (await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken))
                    .Select(oi => oi.UserId)
                    .Distinct()
                    .Count();

                if (participantCount > 0 && restaurant != null)
                {
                    orderHistory.DeliveryFeeShare = restaurant.DeliveryFee / participantCount;
                }

                // Get payment status if filtering by user
                if (shouldFilterByUser && filterUserId.HasValue)
                {
                    var payment = (await _unitOfWork.Payments.FindAsync(
                        p => p.OrderId == order.Id && p.UserId == filterUserId, cancellationToken)).FirstOrDefault();

                    orderHistory.UserPaymentStatus = payment?.Status ?? PaymentStatus.Unpaid;
                }

                decimal userTotal = 0;

                // Process each order item
                foreach (var item in orderItems)
                {
                    var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                    if (menuItem == null) continue;

                    var user = await _unitOfWork.Users.GetByIdAsync(item.UserId, cancellationToken);

                    var orderItemDto = new UserOrderItemDto
                    {
                        Id = item.Id,
                        MenuItemName = menuItem.Name,
                        Price = menuItem.Price,
                        Quantity = item.Quantity,
                        Note = item.Note,
                        Total = menuItem.Price * item.Quantity,
                        UserId = item.UserId,
                        UserName = user?.Name ?? "Unknown User"
                    };

                    orderHistory.UserItems.Add(orderItemDto);

                    // Only add to user total if filtering by user and it's the user's own item
                    if (shouldFilterByUser && filterUserId.HasValue && item.UserId == filterUserId)
                    {
                        userTotal += orderItemDto.Total;
                    }
                }

                // Add delivery fee share to total if filtering by user
                if (shouldFilterByUser && filterUserId.HasValue)
                {
                    orderHistory.UserTotal = userTotal + orderHistory.DeliveryFeeShare;
                }

                result.Add(orderHistory);
            }

            return Result<List<OrderHistoryDto>>.Success(result);
        }
    }
}