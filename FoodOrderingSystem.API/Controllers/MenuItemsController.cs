using System;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.MenuItems.Commands.CreateMenuItem;
using FoodOrderingSystem.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodOrderingSystem.Application.Features.MenuItems.Queries.GetMenuItemById;
using FoodOrderingSystem.Application.Features.MenuItems.Queries.GetMenuItemsByRestaurantId;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.API.Controllers
{
    public class MenuItemsController : BaseController
    {
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CreateMenuItemCommand command)
        {
            var result = await Mediator.Send(command);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await Mediator.Send(new GetMenuItemByIdQuery { Id = id });
            
            if (!result.Succeeded)
                return NotFound(result.Errors);
                
            return Ok(result.Data);
        }

        [HttpGet("restaurant/{restaurantId}")]
        public async Task<IActionResult> GetByRestaurantId(Guid restaurantId)
        {
            var result = await Mediator.Send(new GetMenuItemsByRestaurantIdQuery { RestaurantId = restaurantId });
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(Guid id, UpdateMenuItemCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("The ID in the URL does not match the ID in the request body.");
            }

            var result = await Mediator.Send(command);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(result.Data);
        }
    }
} 