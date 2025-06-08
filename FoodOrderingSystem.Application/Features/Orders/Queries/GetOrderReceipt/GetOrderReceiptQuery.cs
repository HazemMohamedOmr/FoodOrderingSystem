using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoodOrderingSystem.Application.Common.Interfaces;
using FoodOrderingSystem.Application.Common.Models;
using FoodOrderingSystem.Domain.Enums;
using MediatR;

namespace FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderReceipt
{
    public class GetOrderReceiptQuery : IRequest<Result<OrderReceiptDto>>
    {
        public Guid OrderId { get; set; }
    }

    public class OrderReceiptDto
    {
        public Guid OrderId { get; set; }
        public string RestaurantName { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderReceiptItemDto> Items { get; set; } = new List<OrderReceiptItemDto>();
        public decimal DeliveryFee { get; set; }
        public decimal GrandTotal { get; set; }
        public int UserCount { get; set; }
        public decimal DeliveryFeePerUser { get; set; }
    }

    public class OrderReceiptItemDto
    {
        public int UserNumber { get; set; }
        public string UserName { get; set; }
        public string UserPhoneNumber { get; set; }
        public List<OrderItemDetailDto> Items { get; set; } = new List<OrderItemDetailDto>();
        public decimal DeliveryFeeShare { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class OrderItemDetailDto
    {
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Note { get; set; }
        public decimal Total { get; set; }
    }

    public class GetOrderReceiptQueryHandler : IRequestHandler<GetOrderReceiptQuery, Result<OrderReceiptDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetOrderReceiptQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<OrderReceiptDto>> Handle(GetOrderReceiptQuery request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                return Result<OrderReceiptDto>.Failure($"Order with ID {request.OrderId} not found.");
            }

            if (order.Status != OrderStatus.Closed)
            {
                return Result<OrderReceiptDto>.Failure("Cannot generate receipt for an open order.");
            }

            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(order.RestaurantId, cancellationToken);
            if (restaurant == null)
            {
                return Result<OrderReceiptDto>.Failure("Restaurant not found.");
            }

            var receipt = new OrderReceiptDto
            {
                OrderId = order.Id,
                RestaurantName = restaurant.Name,
                OrderDate = order.OrderDate,
                DeliveryFee = restaurant.DeliveryFee
            };

            // Get all order items
            var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id, cancellationToken);
            
            // Group by user
            var userGroups = orderItems.GroupBy(oi => oi.UserId).ToList();
            receipt.UserCount = userGroups.Count;
            
            if (receipt.UserCount > 0)
            {
                receipt.DeliveryFeePerUser = restaurant.DeliveryFee / receipt.UserCount;
            }

            int userNumber = 1;
            foreach (var userGroup in userGroups)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userGroup.Key, cancellationToken);
                if (user == null) continue;

                var receiptItem = new OrderReceiptItemDto
                {
                    UserNumber = userNumber++,
                    UserName = user.Name,
                    UserPhoneNumber = user.PhoneNumber,
                    DeliveryFeeShare = receipt.DeliveryFeePerUser
                };

                decimal userSubtotal = 0;
                
                foreach (var orderItem in userGroup)
                {
                    var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(orderItem.MenuItemId, cancellationToken);
                    if (menuItem == null) continue;

                    var itemDetail = new OrderItemDetailDto
                    {
                        MenuItemName = menuItem.Name,
                        Quantity = orderItem.Quantity,
                        Price = menuItem.Price,
                        Note = orderItem.Note,
                        Total = menuItem.Price * orderItem.Quantity
                    };

                    userSubtotal += itemDetail.Total;
                    receiptItem.Items.Add(itemDetail);
                }

                receiptItem.Subtotal = userSubtotal + receipt.DeliveryFeePerUser;
                receipt.Items.Add(receiptItem);
                receipt.GrandTotal += receiptItem.Subtotal;
            }

            return Result<OrderReceiptDto>.Success(receipt);
        }
    }
} 