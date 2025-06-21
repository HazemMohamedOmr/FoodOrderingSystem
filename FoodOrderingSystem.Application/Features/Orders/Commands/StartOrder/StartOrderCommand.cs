using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using FoodOrderingSystem.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.StartOrder
{
    public class StartOrderCommand : IRequest<Result<Guid>>
    {
        public Guid RestaurantId { get; set; }
    }

    public class StartOrderCommandHandler : IRequestHandler<StartOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public StartOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(StartOrderCommand request, CancellationToken cancellationToken)
        {
            // Get current user ID
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<Guid>.Failure("User ID not found in the token.");
            }

            if (!Guid.TryParse(userId, out Guid managerId))
            {
                return Result<Guid>.Failure("Invalid user ID format.");
            }

            // Validate restaurant
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.RestaurantId, cancellationToken);
            if (restaurant == null)
            {
                return Result<Guid>.Failure($"Restaurant with ID {request.RestaurantId} not found.");
            }

            // Validate manager
            var manager = await _unitOfWork.Users.GetByIdAsync(managerId, cancellationToken);
            if (manager == null)
            {
                return Result<Guid>.Failure($"Manager with ID {managerId} not found.");
            }

            // Only managers and admins can start orders
            if (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin)
            {
                return Result<Guid>.Failure("Only managers or admins can start orders.");
            }

            var userName = _currentUserService.UserName ?? manager.Name ?? "System";

            var order = new Order
            {
                RestaurantId = request.RestaurantId,
                ManagerId = managerId,
                Status = OrderStatus.Open,
                CreatedAt = DateTime.UtcNow,
                OrderDate = DateTime.UtcNow,
                CreatedBy = userName,
                LastModifiedBy = userName,
                LastModifiedAt = DateTime.UtcNow
            };

            await _unitOfWork.Orders.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notifications
            await _notificationService.SendOrderStartNotificationAsync(order, cancellationToken);

            return Result<Guid>.Success(order.Id);
        }
    }
} 