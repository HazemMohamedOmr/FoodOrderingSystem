using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Commands.UpdatePaymentStatus
{
    public class UpdatePaymentStatusCommand : IRequest<Result>
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public PaymentStatus Status { get; set; }
        public Guid ManagerId { get; set; }
    }

    public class UpdatePaymentStatusCommandHandler : IRequestHandler<UpdatePaymentStatusCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdatePaymentStatusCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(UpdatePaymentStatusCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result.Failure($"Order with ID {request.OrderId} not found.");
            }

            if (order.ManagerId != request.ManagerId)
            {
                var manager = await _unitOfWork.Users.GetByIdAsync(request.ManagerId, cancellationToken);
                if (manager == null || manager.Role != UserRole.Admin)
                {
                    return Result.Failure("Only the manager who started the order or an admin can update payment status.");
                }
            }

            var payments = (await _unitOfWork.Payments.FindAsync(p => 
                p.OrderId == request.OrderId && p.UserId == request.UserId, cancellationToken)).ToList();

            if (payments.Count == 0)
            {
                return Result.Failure($"No payment record found for user {request.UserId} in order {request.OrderId}.");
            }

            var userName = _currentUserService.UserName ?? "System";

            foreach (var payment in payments)
            {
                payment.Status = request.Status;
                payment.LastModifiedAt = DateTime.UtcNow;
                payment.LastModifiedBy = userName;
                _unitOfWork.Payments.Update(payment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
} 