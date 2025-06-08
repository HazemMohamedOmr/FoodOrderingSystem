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
        public Guid ManagerId { get; set; }
    }

    public class CloseOrderCommandHandler : IRequestHandler<CloseOrderCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public CloseOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<Result> Handle(CloseOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result.Failure($"Order with ID {request.OrderId} not found.");
            }

            if (order.Status == OrderStatus.Closed)
            {
                return Result.Failure("Order is already closed.");
            }

            if (order.ManagerId != request.ManagerId)
            {
                var manager = await _unitOfWork.Users.GetByIdAsync(request.ManagerId, cancellationToken);
                if (manager == null || manager.Role != UserRole.Admin)
                {
                    return Result.Failure("Only the manager who started the order or an admin can close it.");
                }
            }

            order.Status = OrderStatus.Closed;
            order.ClosedAt = DateTime.UtcNow;
            order.LastModifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notifications
            await _notificationService.SendOrderCloseNotificationAsync(order, cancellationToken);

            return Result.Success();
        }
    }
}