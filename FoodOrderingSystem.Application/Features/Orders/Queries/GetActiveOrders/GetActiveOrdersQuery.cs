using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetActiveOrders
{
    public class GetActiveOrdersQuery : IRequest<Result<List<OrderDto>>>
    {
    }

    public class GetActiveOrdersQueryHandler : IRequestHandler<GetActiveOrdersQuery, Result<List<OrderDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetActiveOrdersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<OrderDto>>> Handle(GetActiveOrdersQuery request, CancellationToken cancellationToken)
        {
            var activeOrders = await _unitOfWork.Orders.FindAsync(o => o.Status == OrderStatus.Open, cancellationToken);
            var orderedOrders = activeOrders.OrderByDescending(o => o.CreatedAt).ToList();
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