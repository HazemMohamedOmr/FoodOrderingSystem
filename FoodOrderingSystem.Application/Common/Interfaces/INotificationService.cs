using System;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Domain.Entities;

namespace FoodOrderingSystem.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task SendOrderStartNotificationAsync(Order order, CancellationToken cancellationToken = default);
        Task SendOrderCloseNotificationAsync(Order order, CancellationToken cancellationToken = default);
        Task SendOrderClosedNotificationAsync(Order order, CancellationToken cancellationToken = default);
        Task SendOrderReceiptAsync(Order order, Guid userId, CancellationToken cancellationToken = default);
        Task SendOrderSummaryToCreatorAsync(Order order, Guid creatorId, CancellationToken cancellationToken = default);
    }
}