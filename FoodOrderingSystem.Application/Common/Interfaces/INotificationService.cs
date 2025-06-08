using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderingSystem.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task<Result> SendOrderStartNotificationAsync(Order order, CancellationToken cancellationToken = default);
        Task<Result> SendOrderCloseNotificationAsync(Order order, CancellationToken cancellationToken = default);
        //Task SendWhatsAppMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
        //Task SendEmailAsync(string email, string subject, string message, CancellationToken cancellationToken = default);
        //Task NotifyUsersAboutOrderAsync(string orderId, string message, CancellationToken cancellationToken = default);
        Task<Result> SendOrderReceiptAsync(Order order, Guid userId, CancellationToken cancellationToken = default);
    }
}