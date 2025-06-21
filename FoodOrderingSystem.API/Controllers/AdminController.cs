using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.Users.Queries.GetAllUsers;
using FoodOrderingSystem.Application.Features.Users.Commands.UpdateUserRole;
using FoodOrderingSystem.Domain.Enums;

namespace FoodOrderingSystem.API.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        [HttpGet("profile")]
        public IActionResult GetAdminProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var phone = User.FindFirstValue(ClaimTypes.MobilePhone);

            var profile = new
            {
                Id = userId,
                Name = name,
                Email = email,
                Role = role,
                PhoneNumber = phone
            };

            return Ok(profile);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await Mediator.Send(new GetAllUsersQuery());
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(result.Data);
        }
        
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
        {
            var command = new UpdateUserRoleCommand
            {
                UserId = userId,
                NewRole = request.Role
            };
            
            var result = await Mediator.Send(command);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(new { message = $"User role updated to {request.Role}" });
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "This endpoint is only accessible to administrators" });
        }
    }
    
    public class UpdateUserRoleRequest
    {
        public UserRole Role { get; set; }
    }
} 