using System;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.MenuItems.Commands.CreateMenuItem;
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
                
            return Ok(result.Data);
        }
    }
} 