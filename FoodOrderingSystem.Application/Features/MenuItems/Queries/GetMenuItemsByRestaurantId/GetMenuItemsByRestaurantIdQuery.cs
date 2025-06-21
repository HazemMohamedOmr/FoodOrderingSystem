using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.MenuItems.Queries.GetMenuItemsByRestaurantId
{
    public class GetMenuItemsByRestaurantIdQuery : IRequest<Result<List<MenuItemDto>>>
    {
        public Guid RestaurantId { get; set; }
    }

    public class GetMenuItemsByRestaurantIdQueryHandler : IRequestHandler<GetMenuItemsByRestaurantIdQuery, Result<List<MenuItemDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMenuItemsByRestaurantIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<MenuItemDto>>> Handle(GetMenuItemsByRestaurantIdQuery request, CancellationToken cancellationToken)
        {
            // First check if the restaurant exists
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.RestaurantId, cancellationToken);
            
            if (restaurant == null)
            {
                return Result<List<MenuItemDto>>.Failure($"Restaurant with ID {request.RestaurantId} not found.");
            }

            var menuItems = await _unitOfWork.MenuItems.FindAsync(mi => mi.RestaurantId == request.RestaurantId, cancellationToken);
            var menuItemDtos = _mapper.Map<List<MenuItemDto>>(menuItems);

            return Result<List<MenuItemDto>>.Success(menuItemDtos);
        }
    }
} 