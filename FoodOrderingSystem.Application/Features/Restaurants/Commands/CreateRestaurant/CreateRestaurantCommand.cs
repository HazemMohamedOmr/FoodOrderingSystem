using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using MediatR;
using System.Security.Claims;

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
        private readonly ICurrentUserService _currentUserService;

        public CreateRestaurantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(CreateRestaurantCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var userName = _currentUserService.UserName ?? "System";

            var restaurant = new Restaurant
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                DeliveryFee = request.DeliveryFee,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userName,
                LastModifiedBy = userName,
                LastModifiedAt = DateTime.UtcNow
            };

            await _unitOfWork.Restaurants.AddAsync(restaurant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(restaurant.Id);
        }
    }
} 