using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;
using System.Linq;
using System.Collections.Generic;

namespace FoodOrderingSystem.Application.Features.Restaurants.Queries.GetRestaurantById
{
    public class GetRestaurantByIdQuery : IRequest<Result<RestaurantDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetRestaurantByIdQueryHandler : IRequestHandler<GetRestaurantByIdQuery, Result<RestaurantDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetRestaurantByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<RestaurantDto>> Handle(GetRestaurantByIdQuery request, CancellationToken cancellationToken)
        {
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.Id, cancellationToken);

            if (restaurant == null)
            {
                return Result<RestaurantDto>.Failure($"Restaurant with ID {request.Id} not found.");
            }

            var restaurantDto = _mapper.Map<RestaurantDto>(restaurant);
            
            // Get menu items for the restaurant
            var menuItems = await _unitOfWork.MenuItems.FindAsync(mi => mi.RestaurantId == request.Id, cancellationToken);
            restaurantDto.MenuItems = _mapper.Map<List<MenuItemDto>>(menuItems);

            return Result<RestaurantDto>.Success(restaurantDto);
        }
    }
} 