using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.UpdateOrderItem
{
    public class UpdateOrderItemCommand : IRequest<Result<Guid>>
    {
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
    }

    public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateOrderItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
        {
            // Get current user ID
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<Guid>.Failure("User ID not found in the token.");
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                return Result<Guid>.Failure("Invalid user ID format.");
            }

            // Get the order item
            var orderItem = await _unitOfWork.OrderItems.GetByIdAsync(request.OrderItemId, cancellationToken);
            if (orderItem == null)
            {
                return Result<Guid>.Failure($"Order item with ID {request.OrderItemId} not found.");
            }

            // Check if the user owns this order item
            if (orderItem.UserId != parsedUserId)
            {
                var userRole = _currentUserService.UserRole;
                // Allow admins and managers to edit any order item
                if (userRole != "Admin" && userRole != "Manager")
                {
                    return Result<Guid>.Failure("You can only update your own order items.");
                }
            }

            // Get the order to check if it's still open
            var order = await _unitOfWork.Orders.GetByIdAsync(orderItem.OrderId, cancellationToken);
            if (order.Status != OrderStatus.Open)
            {
                return Result<Guid>.Failure("Cannot update items in a closed order.");
            }

            var userName = _currentUserService.UserName ?? "System";

            // Update the order item
            orderItem.Quantity = request.Quantity;
            orderItem.Note = request.Note;
            orderItem.LastModifiedAt = DateTime.UtcNow;
            orderItem.LastModifiedBy = userName;

            _unitOfWork.OrderItems.Update(orderItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(orderItem.Id);
        }
    }
} 