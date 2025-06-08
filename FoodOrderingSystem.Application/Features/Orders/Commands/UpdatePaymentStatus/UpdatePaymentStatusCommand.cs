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

        public UpdatePaymentStatusCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            foreach (var payment in payments)
            {
                payment.Status = request.Status;
                payment.LastModifiedAt = DateTime.UtcNow;
                await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
} 