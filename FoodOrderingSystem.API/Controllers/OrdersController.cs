using System;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.Orders.Commands.AddOrderItem;
using FoodOrderingSystem.Application.Features.Orders.Commands.StartOrder;
using FoodOrderingSystem.Application.Features.Orders.Queries.GetOrderById;
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
        
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await Mediator.Send(new GetOrderByIdQuery { Id = id });
            
            if (!result.Succeeded)
                return NotFound(result.Errors);
                
            return Ok(result.Data);
        }
    }
} 