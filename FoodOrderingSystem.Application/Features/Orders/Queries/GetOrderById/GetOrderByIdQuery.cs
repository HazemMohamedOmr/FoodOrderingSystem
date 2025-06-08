using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQuery : IRequest<Result<OrderDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetOrderByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, cancellationToken);

            if (order == null)
            {
                return Result<OrderDto>.Failure($"Order with ID {request.Id} not found.");
            }

            var orderDto = _mapper.Map<OrderDto>(order);

            // Get restaurant details
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
            if (restaurant != null)
            {
                orderDto.RestaurantName = restaurant.Name;
                orderDto.DeliveryFee = restaurant.DeliveryFee;
            }

            // Get manager details
            var manager = await _unitOfWork.Users.GetByIdAsync(order.ManagerId, cancellationToken);
            if (manager != null)
            {
                orderDto.ManagerName = manager.Name;
            }

            // Get order items
            var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
            var orderItemDtos = _mapper.Map<System.Collections.Generic.List<OrderItemDto>>(orderItems);

            // Get payments for each user in the order
            foreach (var orderItemDto in orderItemDtos)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(orderItemDto.UserId, cancellationToken);
                if (user != null)
                {
                    orderItemDto.UserName = user.Name;
                    orderItemDto.UserPhoneNumber = user.PhoneNumber;
                }

                var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(orderItemDto.MenuItemId, cancellationToken);
                if (menuItem != null)
                {
                    orderItemDto.MenuItemName = menuItem.Name;
                    orderItemDto.MenuItemPrice = menuItem.Price;
                    orderItemDto.Subtotal = menuItem.Price * orderItemDto.Quantity;
                }

                var payment = (await _unitOfWork.Payments.FindAsync(p =>
                    p.OrderId == order.Id && p.UserId == orderItemDto.UserId, cancellationToken)).FirstOrDefault();

                if (payment != null)
                {
                    orderItemDto.IsPaid = payment.Status == Domain.Enums.PaymentStatus.Paid;
                }
            }

            // Calculate delivery fee share and total
            if (orderItemDtos.Any())
            {
                var userCount = orderItemDtos.Select(oi => oi.UserId).Distinct().Count();
                var deliveryFeeShare = userCount > 0 ? restaurant.DeliveryFee / userCount : 0;

                foreach (var orderItemDto in orderItemDtos)
                {
                    orderItemDto.DeliveryFeeShare = deliveryFeeShare;
                    orderItemDto.Total = orderItemDto.Subtotal + deliveryFeeShare;
                }

                orderDto.TotalAmount = orderItemDtos.Sum(oi => oi.Total);
            }

            orderDto.OrderItems = orderItemDtos;

            return Result<OrderDto>.Success(orderDto);
        }
    }
}