using System;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.Restaurants.Commands.CreateRestaurant;
using FoodOrderingSystem.Application.Features.Restaurants.Queries.GetAllRestaurants;
using FoodOrderingSystem.Application.Features.Restaurants.Queries.GetRestaurantById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.API.Controllers
{
    public class RestaurantsController : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await Mediator.Send(new GetAllRestaurantsQuery());
            return Ok(result.Data);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await Mediator.Send(new GetRestaurantByIdQuery { Id = id });
            
            if (!result.Succeeded)
                return NotFound(result.Errors);
                
            return Ok(result.Data);
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateRestaurantCommand command)
        {
            var result = await Mediator.Send(command);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
        }
    }
} 