using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using FoodOrderingSystem.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.AddOrderItem
{
    public class AddOrderItemCommand : IRequest<Result<Guid>>
    {
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
    }

    public class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public AddOrderItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
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

            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result<Guid>.Failure($"Order with ID {request.OrderId} not found.");
            }

            if (order.Status != OrderStatus.Open)
            {
                return Result<Guid>.Failure("Cannot add items to a closed order.");
            }

            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(request.MenuItemId, cancellationToken);
            if (menuItem == null)
            {
                return Result<Guid>.Failure($"Menu item with ID {request.MenuItemId} not found.");
            }

            if (menuItem.RestaurantId != order.RestaurantId)
            {
                return Result<Guid>.Failure("Menu item does not belong to the restaurant of this order.");
            }

            var userName = _currentUserService.UserName ?? "System";

            var orderItem = new OrderItem
            {
                OrderId = request.OrderId,
                MenuItemId = request.MenuItemId,
                UserId = parsedUserId,
                Quantity = request.Quantity,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userName,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedBy = userName
            };

            await _unitOfWork.OrderItems.AddAsync(orderItem, cancellationToken);
            
            // Create payment record with default status of unpaid
            var payment = new Payment
            {
                OrderId = request.OrderId,
                UserId = parsedUserId,
                Status = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userName,
                LastModifiedBy = userName,
                LastModifiedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(orderItem.Id);
        }
    }
} 