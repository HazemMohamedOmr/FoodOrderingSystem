using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.CloseOrder
{
    public class CloseOrderCommand : IRequest<Result>
    {
        public Guid OrderId { get; set; }
    }

    public class CloseOrderCommandHandler : IRequestHandler<CloseOrderCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;

        public CloseOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(CloseOrderCommand request, CancellationToken cancellationToken)
        {
            // Get current user ID
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result.Failure("User ID not found in the token.");
            }

            if (!Guid.TryParse(userId, out Guid managerId))
            {
                return Result.Failure("Invalid user ID format.");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result.Failure($"Order with ID {request.OrderId} not found.");
            }

            if (order.Status == OrderStatus.Closed)
            {
                return Result.Failure("Order is already closed.");
            }

            // Check if user is a manager or admin
            var user = await _unitOfWork.Users.GetByIdAsync(managerId, cancellationToken);
            if (user == null)
            {
                return Result.Failure($"User with ID {managerId} not found.");
            }

            if (user.Role != UserRole.Manager && user.Role != UserRole.Admin)
            {
                return Result.Failure("Only managers or admins can close orders.");
            }

            var userName = _currentUserService.UserName ?? user.Name ?? "System";

            order.Status = OrderStatus.Closed;
            order.ClosedAt = DateTime.UtcNow;
            order.LastModifiedAt = DateTime.UtcNow;
            order.LastModifiedBy = userName;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notification about order closure to all participants
            await _notificationService.SendOrderClosedNotificationAsync(order, cancellationToken);

            // Send detailed order summary to the order creator/manager
            await _notificationService.SendOrderSummaryToCreatorAsync(order, managerId, cancellationToken);

            return Result.Success();
        }
    }
}