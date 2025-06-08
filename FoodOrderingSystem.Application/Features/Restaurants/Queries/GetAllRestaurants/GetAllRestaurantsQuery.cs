using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Restaurants.Queries.GetAllRestaurants
{
    public class GetAllRestaurantsQuery : IRequest<Result<List<RestaurantDto>>>
    {
    }

    public class GetAllRestaurantsQueryHandler : IRequestHandler<GetAllRestaurantsQuery, Result<List<RestaurantDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllRestaurantsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<RestaurantDto>>> Handle(GetAllRestaurantsQuery request, CancellationToken cancellationToken)
        {
            var restaurants = await _unitOfWork.Restaurants.GetAllAsync(cancellationToken);
            var restaurantDtos = _mapper.Map<List<RestaurantDto>>(restaurants);

            return Result<List<RestaurantDto>>.Success(restaurantDtos);
        }
    }
}