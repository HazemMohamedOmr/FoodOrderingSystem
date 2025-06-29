using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderPaymentStatuses
{
    public class GetOrderPaymentStatusesQuery : IRequest<Result<OrderPaymentStatusesDto>>
    {
        public Guid OrderId { get; set; }
    }

    public class OrderPaymentStatusesDto
    {
        public Guid OrderId { get; set; }
        public string RestaurantName { get; set; }
        public decimal DeliveryFee { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public List<UserPaymentStatusDto> UserPayments { get; set; } = new List<UserPaymentStatusDto>();
    }

    public class UserPaymentStatusDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
    }

    public class GetOrderPaymentStatusesQueryHandler : IRequestHandler<GetOrderPaymentStatusesQuery, Result<OrderPaymentStatusesDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOrderPaymentStatusesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<OrderPaymentStatusesDto>> Handle(GetOrderPaymentStatusesQuery request, CancellationToken cancellationToken)
        {
            // Get the order
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result<OrderPaymentStatusesDto>.Failure($"Order with ID {request.OrderId} not found.");
            }

            // Get the restaurant
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
            if (restaurant == null)
            {
                return Result<OrderPaymentStatusesDto>.Failure($"Restaurant for order {request.OrderId} not found.");
            }

            // Create the response DTO
            var result = new OrderPaymentStatusesDto
            {
                OrderId = order.Id,
                RestaurantName = restaurant.Name,
                DeliveryFee = restaurant.DeliveryFee,
                OrderDate = order.OrderDate,
                Status = order.Status
            };

            // Get all order items grouped by user
            var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == request.OrderId, cancellationToken);
            var userIds = orderItems.Select(oi => oi.UserId).Distinct().ToList();

            // Calculate delivery fee per user
            decimal deliveryFeePerUser = userIds.Count > 0 
                ? restaurant.DeliveryFee / userIds.Count 
                : 0;

            // Get payment status for each user
            foreach (var userId in userIds)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user == null) continue;

                // Calculate total for this user
                decimal userTotal = 0;
                var userItems = orderItems.Where(oi => oi.UserId == userId).ToList();
                
                foreach (var item in userItems)
                {
                    var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                    if (menuItem == null) continue;
                    
                    userTotal += menuItem.Price * item.Quantity;
                }
                
                userTotal += deliveryFeePerUser;

                // Get payment status
                var payment = (await _unitOfWork.Payments.FindAsync(
                    p => p.OrderId == request.OrderId && p.UserId == userId, 
                    cancellationToken)).FirstOrDefault();
                
                var paymentStatus = payment?.Status ?? PaymentStatus.Unpaid;

                result.UserPayments.Add(new UserPaymentStatusDto
                {
                    UserId = userId,
                    UserName = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    Amount = userTotal,
                    Status = paymentStatus
                });
            }

            return Result<OrderPaymentStatusesDto>.Success(result);
        }
    }
} 