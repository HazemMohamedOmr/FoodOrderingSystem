using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Restaurants.Commands.CreateRestaurant
{
    public class CreateRestaurantCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public decimal DeliveryFee { get; set; }
    }

    public class CreateRestaurantCommandHandler : IRequestHandler<CreateRestaurantCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateRestaurantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<Guid>> Handle(CreateRestaurantCommand request, CancellationToken cancellationToken)
        {
            var restaurant = new Restaurant
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                DeliveryFee = request.DeliveryFee,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Restaurants.AddAsync(restaurant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(restaurant.Id);
        }
    }
} 