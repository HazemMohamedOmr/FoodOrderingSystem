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
using FoodOrderingSystem.Infrastructure.BackgroundJobs;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FoodOrderingSystem.Infrastructure.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly NotificationSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotificationService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IOptions<NotificationSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<NotificationService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task SendOrderStartNotificationAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    return;
                }

                // Get all users to notify
                var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
                var endUsers = users.Where(u => u.Role == Domain.Enums.UserRole.EndUser).ToList();

                foreach (var user in endUsers)
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        string subject = $"New Order Started - {restaurant.Name}";
                        string emailContent = GetOrderStartEmailTemplate(user.Name, restaurant.Name);
                        
                        // Schedule the job using Hangfire
                        _backgroundJobClient.Enqueue<EmailJobs>(x => x.SendEmailAsync(
                            user.Email,
                            subject,
                            emailContent,
                            _settings.EmailSmtpServer,
                            _settings.EmailSmtpPort,
                            _settings.EmailSenderAddress,
                            _settings.EmailPassword,
                            _settings.EmailUsername));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order start notification");
            }
        }

        public async Task SendOrderCloseNotificationAsync(Order order, CancellationToken cancellationToken = default)
        {
            try
            {
                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    return;
                }

                // Get all users who participated in this order
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
                var userIds = orderItems.Select(oi => oi.UserId).Distinct().ToList();

                foreach (var userId in userIds)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                    if (user == null) continue;

                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        string subject = $"Order Closed - {restaurant.Name}";
                        string emailContent = GetOrderCloseEmailTemplate(user.Name, restaurant.Name);
                        
                        // Schedule the job using Hangfire
                        _backgroundJobClient.Enqueue<EmailJobs>(x => x.SendEmailAsync(
                            user.Email,
                            subject,
                            emailContent,
                            _settings.EmailSmtpServer,
                            _settings.EmailSmtpPort,
                            _settings.EmailSenderAddress,
                            _settings.EmailPassword,
                            _settings.EmailUsername));
                    }

                    // Send individual receipt
                    await SendOrderReceiptAsync(order, userId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order close notification");
            }
        }
        
        public async Task SendOrderClosedNotificationAsync(Order order, CancellationToken cancellationToken = default)
        {
            // This is an alias for SendOrderCloseNotificationAsync to match the interface
            await SendOrderCloseNotificationAsync(order, cancellationToken);
        }

        public async Task SendOrderSummaryToCreatorAsync(Order order, Guid creatorId, CancellationToken cancellationToken = default)
        {
            try
            {
                var creator = await _unitOfWork.Users.GetByIdAsync(creatorId, cancellationToken);
                if (creator == null || string.IsNullOrEmpty(creator.Email))
                {
                    _logger.LogWarning("Creator not found or has no email. Cannot send order summary.");
                    return;
                }

                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    _logger.LogWarning("Restaurant not found for order {OrderId}", order.Id);
                    return;
                }

                // Get all order items
                var allOrderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
                if (!allOrderItems.Any())
                {
                    _logger.LogWarning("No items found for order {OrderId}", order.Id);
                    return;
                }

                // Group items by user
                var itemsByUser = new Dictionary<Guid, List<(OrderItem Item, string MenuItemName, decimal Price)>>();
                decimal totalOrderAmount = 0;

                foreach (var item in allOrderItems)
                {
                    var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                    if (menuItem == null) continue;

                    if (!itemsByUser.ContainsKey(item.UserId))
                    {
                        itemsByUser[item.UserId] = new List<(OrderItem, string, decimal)>();
                    }

                    itemsByUser[item.UserId].Add((item, menuItem.Name, menuItem.Price));
                    totalOrderAmount += menuItem.Price * item.Quantity;
                }

                // Add delivery fee
                totalOrderAmount += restaurant.DeliveryFee;

                // Generate summary content
                var summaryBuilder = new StringBuilder();
                
                foreach (var userItems in itemsByUser)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userItems.Key, cancellationToken);
                    string userName = user?.Name ?? "Unknown User";
                    decimal userSubtotal = 0;
                    var userItemsList = userItems.Value;

                    summaryBuilder.Append($@"
                    <div class='user-order'>
                        <h3>{userName}</h3>
                        <table>
                            <thead>
                                <tr>
                                    <th>Item</th>
                                    <th class='text-right'>Qty</th>
                                    <th class='text-right'>Price</th>
                                    <th class='text-right'>Total</th>
                                </tr>
                            </thead>
                            <tbody>");

                    foreach (var (item, name, price) in userItemsList)
                    {
                        decimal itemTotal = price * item.Quantity;
                        userSubtotal += itemTotal;

                        summaryBuilder.Append($@"
                                <tr>
                                    <td>{name}</td>
                                    <td class='text-right'>{item.Quantity}</td>
                                    <td class='text-right'>{price:C}</td>
                                    <td class='text-right'>{itemTotal:C}</td>
                                </tr>");

                        if (!string.IsNullOrEmpty(item.Note))
                        {
                            summaryBuilder.Append($@"
                                <tr>
                                    <td colspan='4' style='border-top: none; padding-top: 0;'>
                                        <span style='font-size: 0.9em; color: #666;'>Note: {item.Note}</span>
                                    </td>
                                </tr>");
                        }
                    }

                    decimal deliveryFeeShare = restaurant.DeliveryFee / itemsByUser.Count;
                    decimal userTotal = userSubtotal + deliveryFeeShare;

                    summaryBuilder.Append($@"
                            </tbody>
                            <tfoot>
                                <tr>
                                    <td colspan='3' class='text-right'><strong>Subtotal:</strong></td>
                                    <td class='text-right'>{userSubtotal:C}</td>
                                </tr>
                                <tr>
                                    <td colspan='3' class='text-right'><strong>Delivery Fee Share:</strong></td>
                                    <td class='text-right'>{deliveryFeeShare:C}</td>
                                </tr>
                                <tr class='total-row'>
                                    <td colspan='3' class='text-right'><strong>Total:</strong></td>
                                    <td class='text-right'>{userTotal:C}</td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                    <div style='margin: 20px 0; border-top: 1px dashed #ddd;'></div>");
                }

                string content = $@"
                <h2>Order Summary - {restaurant.Name}</h2>
                
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                    <p><strong>Restaurant:</strong> {restaurant.Name}</p>
                    <p><strong>Order date:</strong> {order.OrderDate:dddd, MMMM d, yyyy 'at' h:mm tt}</p>
                    <p><strong>Status:</strong> Closed</p>
                    <p><strong>Total participants:</strong> {itemsByUser.Count}</p>
                    <p><strong>Total order amount:</strong> {totalOrderAmount:C}</p>
                </div>
                
                <h3>Detailed Breakdown by User</h3>
                
                {summaryBuilder.ToString()}
                
                <div style='background-color: #e8f3ee; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                    <h4 style='margin-top: 0;'>Collection Instructions</h4>
                    <p>Please collect the payment from each participant according to their individual totals shown above.</p>
                    <p>Total amount to be collected: {totalOrderAmount:C}</p>
                </div>";

                string subject = $"Complete Order Summary - {restaurant.Name}";
                string emailContent = GetEmailBaseTemplate(content);
                
                // Schedule the job using Hangfire
                _backgroundJobClient.Enqueue<EmailJobs>(x => x.SendEmailAsync(
                    creator.Email,
                    subject,
                    emailContent,
                    _settings.EmailSmtpServer,
                    _settings.EmailSmtpPort,
                    _settings.EmailSenderAddress,
                    _settings.EmailPassword,
                    _settings.EmailUsername));

                _logger.LogInformation("Order summary job scheduled for order creator {UserId}", creatorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order summary to creator");
            }
        }

        public async Task SendOrderReceiptAsync(Order order, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return;
                }

                var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
                if (restaurant == null)
                {
                    return;
                }

                // Get user's order items
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id && oi.UserId == userId, cancellationToken);
                if (!orderItems.Any())
                {
                    return;
                }

                // Generate receipt HTML
                StringBuilder receiptHtml = new StringBuilder();
                decimal subtotal = 0;
                
                // Collect receipt items
                var receiptItems = new List<(string name, int quantity, decimal price, string note, decimal total)>();
                
                foreach (var item in orderItems)
                {
                    var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId, cancellationToken);
                    if (menuItem == null) continue;

                    decimal itemTotal = menuItem.Price * item.Quantity;
                    subtotal += itemTotal;
                    
                    receiptItems.Add((menuItem.Name, item.Quantity, menuItem.Price, item.Note ?? "", itemTotal));
                }

                // Calculate delivery fee share
                var allOrderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
                int userCount = allOrderItems.Select(oi => oi.UserId).Distinct().Count();
                decimal deliveryFeeShare = restaurant.DeliveryFee / userCount;
                decimal total = subtotal + deliveryFeeShare;
                
                string receiptHtmlContent = GetReceiptEmailTemplate(
                    user.Name, 
                    restaurant.Name, 
                    order.OrderDate, 
                    receiptItems,
                    subtotal,
                    deliveryFeeShare,
                    total);

                // Send receipt via Email in background
                if (!string.IsNullOrEmpty(user.Email))
                {
                    string subject = $"Your Receipt - {restaurant.Name}";
                    
                    // Schedule the job using Hangfire
                    _backgroundJobClient.Enqueue<EmailJobs>(x => x.SendEmailAsync(
                        user.Email,
                        subject,
                        receiptHtmlContent,
                        _settings.EmailSmtpServer,
                        _settings.EmailSmtpPort,
                        _settings.EmailSenderAddress,
                        _settings.EmailPassword,
                        _settings.EmailUsername));
                    
                    _logger.LogInformation("Receipt email job scheduled for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling receipt email");
            }
        }

        private async Task SendEmailAsync(string email, string subject, string htmlBody, IList<IFormFile>? attachments = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.EmailSmtpServer))
                {
                    _logger.LogWarning("Email settings are not configured. Skipping email notification.");
                    return;
                }

                if (attachments == null || !attachments.Any())
                {
                    // Schedule simple email job
                    _backgroundJobClient.Enqueue<EmailJobs>(x => x.SendEmailAsync(
                        email,
                        subject,
                        htmlBody,
                        _settings.EmailSmtpServer,
                        _settings.EmailSmtpPort,
                        _settings.EmailSenderAddress,
                        _settings.EmailPassword,
                        _settings.EmailUsername));
                }
                else
                {
                    // For emails with attachments, we need to convert IFormFile to our EmailAttachment class
                    var emailAttachments = new List<EmailAttachment>();
                    
                    foreach (var file in attachments)
                    {
                        if (file.Length > 0)
                        {
                            using var ms = new MemoryStream();
                            await file.CopyToAsync(ms);
                            var fileBytes = ms.ToArray();

                            emailAttachments.Add(new EmailAttachment
                            {
                                FileName = file.FileName,
                                FileBytes = fileBytes,
                                ContentType = file.ContentType
                            });
                        }
                    }
                    
                    // Schedule email with attachments job
                    _backgroundJobClient.Enqueue<EmailJobs>(x => x.SendEmailWithAttachmentsAsync(
                        email,
                        subject,
                        htmlBody,
                        emailAttachments,
                        _settings.EmailSmtpServer,
                        _settings.EmailSmtpPort,
                        _settings.EmailSenderAddress,
                        _settings.EmailPassword,
                        _settings.EmailUsername));
                }
                
                _logger.LogInformation("Email job scheduled for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling email to {Email}", email);
            }
        }

        #region Email Templates

        private string GetEmailBaseTemplate(string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Food Ordering System</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f8f9fa;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }}
        .header {{
            background-color: #4267b2;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
            margin: -20px -20px 20px -20px;
        }}
        .content {{
            padding: 20px 0;
        }}
        .footer {{
            text-align: center;
            padding-top: 20px;
            color: #777;
            font-size: 0.9em;
            border-top: 1px solid #eee;
            margin-top: 20px;
        }}
        .btn {{
            display: inline-block;
            background-color: #4267b2;
            color: white;
            text-decoration: none;
            padding: 10px 20px;
            border-radius: 5px;
            font-weight: bold;
            margin: 15px 0;
        }}
        h1, h2, h3 {{
            color: #333;
            margin-bottom: 15px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 25px 0;
        }}
        th, td {{
            padding: 10px;
            border: 1px solid #ddd;
            text-align: left;
        }}
        th {{
            background-color: #f8f9fa;
            font-weight: bold;
        }}
        .highlight {{
            background-color: #f0f7ff;
        }}
        .text-right {{
            text-align: right;
        }}
        .total-row {{
            font-weight: bold;
            background-color: #eef2ff;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Food Ordering System</h1>
        </div>
        <div class='content'>
            {content}
        </div>
        <div class='footer'>
            <p>This is an automated message from the Food Ordering System.</p>
            <p>&copy; {DateTime.Now.Year} Food Ordering System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetOrderStartEmailTemplate(string userName, string restaurantName)
        {
            string content = $@"
<h2>Hello {userName},</h2>

<p>A new order has been started at <strong>{restaurantName}</strong>.</p>

<p>The order is now open for you to add your items. Don't miss out - add your order items now!</p>

<p style='text-align: center;'>
    <a href='#' class='btn'>View Order</a>
</p>

<p>If you have any questions, please contact the order coordinator.</p>

<p>Thank you for using our Food Ordering System!</p>";

            return GetEmailBaseTemplate(content);
        }

        private string GetOrderCloseEmailTemplate(string userName, string restaurantName)
        {
            string content = $@"
<h2>Hello {userName},</h2>

<p>The group order from <strong>{restaurantName}</strong> has been closed.</p>

<p>Your order receipt has been sent to you in a separate email with a detailed breakdown of your items and payment information.</p>

<p>Please check your receipt carefully and arrange for payment according to the instructions provided.</p>

<p>Thank you for using our Food Ordering System!</p>";

            return GetEmailBaseTemplate(content);
        }

        private string GetReceiptEmailTemplate(
            string userName, 
            string restaurantName, 
            DateTime orderDate,
            List<(string name, int quantity, decimal price, string note, decimal total)> items,
            decimal subtotal,
            decimal deliveryFeeShare,
            decimal total)
        {
            StringBuilder itemsHtml = new StringBuilder();
            
            foreach (var item in items)
            {
                itemsHtml.AppendLine($@"
<tr>
    <td>{item.name}</td>
    <td class='text-right'>{item.quantity}</td>
    <td class='text-right'>{item.price:C}</td>
    <td class='text-right'>{item.total:C}</td>
</tr>");
                
                if (!string.IsNullOrEmpty(item.note))
                {
                    itemsHtml.AppendLine($@"
<tr>
    <td colspan='4' style='border-top: none; padding-top: 0;'>
        <span style='font-size: 0.9em; color: #666;'>Note: {item.note}</span>
    </td>
</tr>");
                }
            }

            string content = $@"
<h2>Your Order Receipt</h2>

<div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
    <p><strong>Order from:</strong> {restaurantName}</p>
    <p><strong>Order date:</strong> {orderDate:dddd, MMMM d, yyyy 'at' h:mm tt}</p>
    <p><strong>Customer:</strong> {userName}</p>
</div>

<h3>Order Details</h3>

<table>
    <thead>
        <tr>
            <th>Item</th>
            <th class='text-right'>Qty</th>
            <th class='text-right'>Price</th>
            <th class='text-right'>Total</th>
        </tr>
    </thead>
    <tbody>
        {itemsHtml}
    </tbody>
    <tfoot>
        <tr>
            <td colspan='3' class='text-right'><strong>Subtotal:</strong></td>
            <td class='text-right'>{subtotal:C}</td>
        </tr>
        <tr>
            <td colspan='3' class='text-right'><strong>Delivery Fee Share:</strong></td>
            <td class='text-right'>{deliveryFeeShare:C}</td>
        </tr>
        <tr class='total-row'>
            <td colspan='3' class='text-right'><strong>Total:</strong></td>
            <td class='text-right'>{total:C}</td>
        </tr>
    </tfoot>
</table>

<div style='background-color: #e8f3ee; padding: 15px; border-radius: 5px; margin-top: 20px;'>
    <h4 style='margin-top: 0;'>Payment Instructions</h4>
    <p>Please arrange payment to the order coordinator as soon as possible.</p>
</div>

<p>Thank you for your order!</p>";

            return GetEmailBaseTemplate(content);
        }

        #endregion
    }
}