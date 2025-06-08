using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

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
            return Result<RestaurantDto>.Success(restaurantDto);
        }
    }
} 