using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderHistory
{
    public class GetOrderHistoryQuery : IRequest<Result<List<OrderDto>>>
    {
        public Guid? UserId { get; set; }
        public Guid? RestaurantId { get; set; }
    }

    public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, Result<List<OrderDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetOrderHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<OrderDto>>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
        {
            var orders = await _unitOfWork.Orders.GetAllAsync(cancellationToken);
            var filteredOrders = orders.AsQueryable();

            if (request.RestaurantId.HasValue)
            {
                filteredOrders = filteredOrders.Where(o => o.RestaurantId == request.RestaurantId);
            }

            if (request.UserId.HasValue)
            {
                // If UserId is provided, filter orders where the user participated
                var userOrderItems = await _unitOfWork.OrderItems.FindAsync(
                    oi => oi.UserId == request.UserId, cancellationToken);
                
                var userOrderIds = userOrderItems.Select(oi => oi.OrderId).Distinct();
                filteredOrders = filteredOrders.Where(o => userOrderIds.Contains(o.Id));
            }

            // Order by date descending
            var orderedOrders = filteredOrders.OrderByDescending(o => o.CreatedAt).ToList();
            var orderDtos = _mapper.Map<List<OrderDto>>(orderedOrders);

            // Populate restaurant and manager names
            foreach (var orderDto in orderDtos)
            {
                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(orderDto.RestaurantId, cancellationToken);
                if (restaurant != null)
                {
                    orderDto.RestaurantName = restaurant.Name;
                    orderDto.DeliveryFee = restaurant.DeliveryFee;
                }

                var manager = await _unitOfWork.Users.GetByIdAsync(orderDto.ManagerId, cancellationToken);
                if (manager != null)
                {
                    orderDto.ManagerName = manager.Name;
                }
            }

            return Result<List<OrderDto>>.Success(orderDtos);
        }
    }
} 