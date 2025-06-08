using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.StartOrder
{
    public class StartOrderCommand : IRequest<Result<Guid>>
    {
        public Guid RestaurantId { get; set; }
        public Guid ManagerId { get; set; }
    }

    public class StartOrderCommandHandler : IRequestHandler<StartOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public StartOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<Result<Guid>> Handle(StartOrderCommand request, CancellationToken cancellationToken)
        {
            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(request.RestaurantId, cancellationToken);
            if (restaurant == null)
            {
                return Result<Guid>.Failure($"Restaurant with ID {request.RestaurantId} not found.");
            }

            var manager = await _unitOfWork.Users.GetByIdAsync(request.ManagerId, cancellationToken);
            if (manager == null)
            {
                return Result<Guid>.Failure($"Manager with ID {request.ManagerId} not found.");
            }

            if (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin)
            {
                return Result<Guid>.Failure("Only managers or admins can start orders.");
            }

            var order = new Order
            {
                RestaurantId = request.RestaurantId,
                ManagerId = request.ManagerId,
                Status = OrderStatus.Open,
                CreatedAt = DateTime.UtcNow,
                OrderDate = DateTime.UtcNow
            };

            await _unitOfWork.Orders.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notifications
            await _notificationService.SendOrderStartNotificationAsync(order, cancellationToken);

            return Result<Guid>.Success(order.Id);
        }
    }
} 