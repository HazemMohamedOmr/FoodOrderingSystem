using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.Orders.Commands.AddOrderItem;
using FoodOrderingSystem.Application.Features.Orders.Commands.CloseOrder;
using FoodOrderingSystem.Application.Features.Orders.Commands.DeleteOrderItem;
using FoodOrderingSystem.Application.Features.Orders.Commands.StartOrder;
using FoodOrderingSystem.Application.Features.Orders.Commands.UpdateOrderItem;
using FoodOrderingSystem.Application.Features.Orders.Commands.UpdatePaymentStatus;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetActiveOrders;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetMyOrderItems;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderById;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderHistory;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderItems;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderPaymentStatuses;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderReceipt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.API.Controllers
{
    public class OrdersController : BaseController
    {
        [HttpPost("start")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> StartOrder(StartOrderCommand command)
        {
            var result = await Mediator.Send(command);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
        }

        [HttpPost("items")]
        [Authorize]
        public async Task<IActionResult> AddOrderItem(AddOrderItemCommand command)
        {
            var result = await Mediator.Send(command);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }

        [HttpPut("items/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderItem(Guid id, UpdateOrderItemCommand command)
        {
            if (id != command.OrderItemId)
            {
                return BadRequest("The ID in the URL does not match the ID in the request body.");
            }

            var result = await Mediator.Send(command);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await Mediator.Send(new GetOrderByIdQuery { Id = id });

            if (!result.Succeeded)
                return NotFound(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("{id}/items")]
        [Authorize]
        public async Task<IActionResult> GetOrderItems(Guid id)
        {
            var result = await Mediator.Send(new GetOrderItemsQuery { OrderId = id });

            if (!result.Succeeded)
                return NotFound(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("{id}/payment-statuses")]
        [Authorize]
        public async Task<IActionResult> GetOrderPaymentStatuses(Guid id)
        {
            var result = await Mediator.Send(new GetOrderPaymentStatusesQuery { OrderId = id });

            if (!result.Succeeded)
                return NotFound(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("{id}/receipt")]
        [Authorize]
        public async Task<IActionResult> GetOrderReceipt(Guid id)
        {
            var result = await Mediator.Send(new GetOrderReceiptQuery { OrderId = id });

            if (!result.Succeeded)
                return NotFound(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActiveOrders()
        {
            var result = await Mediator.Send(new GetActiveOrdersQuery());

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetOrderHistory([FromQuery] GetOrderHistoryQuery query)
        {
            // If no specific filters are provided, set ShowAllOrders to true
            if (!query.UserId.HasValue && !query.RestaurantId.HasValue)
            {
                query.ShowAllOrders = true;
            }

            var result = await Mediator.Send(query);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }

        [HttpGet("my-history")]
        [Authorize]
        public async Task<IActionResult> GetMyOrderHistory([FromQuery] Guid? restaurantId = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out Guid userId))
            {
                return BadRequest("Invalid or missing user ID in token.");
            }

            var query = new GetOrderHistoryQuery
            {
                UserId = userId,
                RestaurantId = restaurantId,
                IncludeOtherParticipants = false // Only include the current user's items
            };

            var result = await Mediator.Send(query);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }

        [HttpPost("{id}/close")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CloseOrder(Guid id)
        {
            var result = await Mediator.Send(new CloseOrderCommand { OrderId = id });

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Order closed successfully" });
        }

        [HttpPut("{orderId}/users/{userId}/payment-status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdatePaymentStatus(Guid orderId, Guid userId, [FromBody] UpdatePaymentStatusRequest request)
        {
            var currentUserId = User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(currentUserId, out Guid managerId))
            {
                return BadRequest("Invalid user ID format.");
            }

            var command = new UpdatePaymentStatusCommand
            {
                OrderId = orderId,
                UserId = userId,
                Status = request.Status,
                ManagerId = managerId
            };

            var result = await Mediator.Send(command);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Payment status updated successfully" });
        }

        [HttpGet("{id}/my-items")]
        [Authorize]
        public async Task<IActionResult> GetMyOrderItems(Guid id)
        {
            var result = await Mediator.Send(new GetMyOrderItemsQuery { OrderId = id });
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(result.Data);
        }
        
        [HttpDelete("items/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrderItem(Guid id)
        {
            var result = await Mediator.Send(new DeleteOrderItemCommand { OrderItemId = id });
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(new { message = "Order item deleted successfully" });
        }
    }

    public class UpdatePaymentStatusRequest
    {
        public Domain.Enums.PaymentStatus Status { get; set; }
    }
}