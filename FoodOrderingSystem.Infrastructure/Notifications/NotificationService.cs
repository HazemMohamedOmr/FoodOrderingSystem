using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodOrderingSystem.Infrastructure.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly NotificationSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IOptions<NotificationSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<Result> SendOrderStartNotificationAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    return Result.Failure("Restaurant not found.");
                }

                // Get all users to notify
                var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
                var endUsers = users.Where(u => u.Role == Domain.Enums.UserRole.EndUser).ToList();

                foreach (var user in endUsers)
                {
                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        await SendWhatsAppMessageAsync(
                            user.PhoneNumber,
                            $"New order started at {restaurant.Name}! Order is now open for you to add your items.",
                            cancellationToken);
                    }

                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await SendEmailAsync(
                            user.Email,
                            $"New Order Started - {restaurant.Name}",
                            $"Hello {user.Name},\n\nA new order has been started at {restaurant.Name}. The order is now open for you to add your items.\n\nRegards,\nFood Ordering System",
                            cancellationToken);
                    }
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order start notification");
                return Result.Failure("Failed to send notifications. Please check the logs for more details.");
            }
        }

        public async Task<Result> SendOrderCloseNotificationAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    return Result.Failure("Restaurant not found.");
                }

                // Get all users who participated in this order
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
                var userIds = orderItems.Select(oi => oi.UserId).Distinct().ToList();

                foreach (var userId in userIds)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                    if (user == null) continue;

                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        await SendWhatsAppMessageAsync(
                            user.PhoneNumber,
                            $"Order from {restaurant.Name} has been closed. Check your receipt for payment details.",
                            cancellationToken);
                    }

                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await SendEmailAsync(
                            user.Email,
                            $"Order Closed - {restaurant.Name}",
                            $"Hello {user.Name},\n\nThe order from {restaurant.Name} has been closed. Please check your receipt for payment details.\n\nRegards,\nFood Ordering System",
                            cancellationToken);
                    }

                    // Send individual receipt
                    await SendOrderReceiptAsync(order, userId, cancellationToken);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order close notification");
                return Result.Failure("Failed to send notifications. Please check the logs for more details.");
            }
        }

        public async Task<Result> SendOrderReceiptAsync(Order order, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return Result.Failure($"User with ID {userId} not found.");
                }

                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    return Result.Failure("Restaurant not found.");
                }

                // Get user's order items
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id && oi.UserId == userId, cancellationToken);
                if (!orderItems.Any())
                {
                    return Result.Failure("No order items found for this user.");
                }

                // Generate receipt
                var sb = new StringBuilder();
                sb.AppendLine($"Receipt for {restaurant.Name} - {order.OrderDate:yyyy-MM-dd HH:mm}");
                sb.AppendLine($"User: {user.Name}");
                sb.AppendLine();

                decimal subtotal = 0;

                foreach (var item in orderItems)
                {
                    var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                    if (menuItem == null) continue;

                    decimal itemTotal = menuItem.Price * item.Quantity;
                    subtotal += itemTotal;

                    sb.AppendLine($"{menuItem.Name} x{item.Quantity} - {menuItem.Price:C} each = {itemTotal:C}");
                    if (!string.IsNullOrEmpty(item.Note))
                    {
                        sb.AppendLine($"  Note: {item.Note}");
                    }
                }

                // Calculate delivery fee share
                var allOrderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
                int userCount = allOrderItems.Select(oi => oi.UserId).Distinct().Count();
                decimal deliveryFeeShare = restaurant.DeliveryFee / userCount;

                sb.AppendLine();
                sb.AppendLine($"Subtotal: {subtotal:C}");
                sb.AppendLine($"Delivery Fee Share: {deliveryFeeShare:C}");
                sb.AppendLine($"Total: {(subtotal + deliveryFeeShare):C}");

                string receiptText = sb.ToString();

                // Send receipt via WhatsApp
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await SendWhatsAppMessageAsync(user.PhoneNumber, receiptText, cancellationToken);
                }

                // Send receipt via Email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await SendEmailAsync(
                        user.Email,
                        $"Your Receipt - {restaurant.Name}",
                        receiptText,
                        cancellationToken);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order receipt");
                return Result.Failure("Failed to send receipt. Please check the logs for more details.");
            }
        }

        private async Task SendWhatsAppMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            try
            {
                // This is a simplified implementation. In a real-world scenario, you would use the WhatsApp Business API
                // or a third-party service like Twilio to send WhatsApp messages.
                
                if (string.IsNullOrEmpty(_settings.WhatsAppApiUrl) || string.IsNullOrEmpty(_settings.WhatsAppApiKey))
                {
                    _logger.LogWarning("WhatsApp API settings are not configured. Skipping WhatsApp notification.");
                    return;
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.WhatsAppApiKey}");

                var payload = new
                {
                    recipient = phoneNumber,
                    sender = _settings.WhatsAppSenderId,
                    message = message
                };

                var response = await httpClient.PostAsJsonAsync(_settings.WhatsAppApiUrl, payload, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to send WhatsApp message. Status: {Status}, Error: {Error}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {PhoneNumber}", phoneNumber);
            }
        }

        private async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken)
        {
            try
            {
                // This is a simplified implementation. In a real-world scenario, you would use a proper email service
                // like SendGrid, MailKit, etc.
                
                if (string.IsNullOrEmpty(_settings.EmailSmtpServer))
                {
                    _logger.LogWarning("Email settings are not configured. Skipping email notification.");
                    return;
                }

                // In a real implementation, you would create an email client and send the email
                _logger.LogInformation("Sending email to {Email} with subject '{Subject}'", email, subject);
                
                // Simulate sending an email
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", email);
            }
        }
    }
}