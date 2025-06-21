using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Application.Features.Restaurants.Commands.UpdateRestaurant
{
    public class UpdateRestaurantCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public decimal DeliveryFee { get; set; }
    }

    public class UpdateRestaurantCommandHandler : IRequestHandler<UpdateRestaurantCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateRestaurantCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(UpdateRestaurantCommand request, CancellationToken cancellationToken)
        {
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.Id, cancellationToken);

            if (restaurant == null)
            {
                return Result<Guid>.Failure($"Restaurant with ID {request.Id} not found.");
            }

            var userName = _currentUserService.UserName ?? "System";

            restaurant.Name = request.Name;
            restaurant.Description = request.Description;
            restaurant.Address = request.Address;
            restaurant.PhoneNumber = request.PhoneNumber;
            restaurant.DeliveryFee = request.DeliveryFee;
            restaurant.LastModifiedAt = DateTime.UtcNow;
            restaurant.LastModifiedBy = userName;

            _unitOfWork.Restaurants.Update(restaurant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(restaurant.Id);
        }
    }
} 