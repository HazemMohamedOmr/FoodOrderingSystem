using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.DeleteOrderItem
{
    public class DeleteOrderItemCommand : IRequest<Result>
    {
        public Guid OrderItemId { get; set; }
    }
    
    public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        
        public DeleteOrderItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }
        
        public async Task<Result> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
        {
            // Get current user ID
            var userIdString = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Result.Failure("User ID not found or invalid.");
            }
            
            // Get the order item
            var orderItem = await _unitOfWork.OrderItems.GetByIdAsync(request.OrderItemId, cancellationToken);
            if (orderItem == null)
            {
                return Result.Failure($"Order item with ID {request.OrderItemId} not found.");
            }
            
            // Check if the order item belongs to the current user
            if (orderItem.UserId != userId)
            {
                return Result.Failure("You can only delete your own order items.");
            }
            
            // Get the order to check if it's still active
            var order = await _unitOfWork.Orders.GetByIdAsync(orderItem.OrderId, cancellationToken);
            if (order == null)
            {
                return Result.Failure($"Order with ID {orderItem.OrderId} not found.");
            }
            
            // Check if the order is still open/active
            if (order.Status != OrderStatus.Open)
            {
                return Result.Failure("Cannot delete items from a closed order.");
            }
            
            // Delete the order item
            _unitOfWork.OrderItems.Delete(orderItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Success();
        }
    }
} 