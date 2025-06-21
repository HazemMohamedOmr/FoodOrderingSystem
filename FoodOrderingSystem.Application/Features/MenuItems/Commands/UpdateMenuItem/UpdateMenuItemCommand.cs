using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using MediatR;

namespace FoodOrderingSystem.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public Guid RestaurantId { get; set; }
    }

    public class UpdateMenuItemCommandHandler : IRequestHandler<UpdateMenuItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateMenuItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(request.Id, cancellationToken);

            if (menuItem == null)
            {
                return Result<Guid>.Failure($"Menu item with ID {request.Id} not found.");
            }

            // Check if the restaurant exists
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.RestaurantId, cancellationToken);

            if (restaurant == null)
            {
                return Result<Guid>.Failure($"Restaurant with ID {request.RestaurantId} not found.");
            }

            var userName = _currentUserService.UserName ?? "System";

            menuItem.Name = request.Name;
            menuItem.Description = request.Description;
            menuItem.Price = request.Price;
            menuItem.RestaurantId = request.RestaurantId;
            menuItem.LastModifiedAt = DateTime.UtcNow;
            menuItem.LastModifiedBy = userName;

            _unitOfWork.MenuItems.Update(menuItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(menuItem.Id);
        }
    }
} 