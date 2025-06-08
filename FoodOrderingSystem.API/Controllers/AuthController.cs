using System.Threading.Tasks;
using FoodOrderingSystem.Application.Features.Users.Commands.LoginUser;
using FoodOrderingSystem.Application.Features.Users.Commands.RegisterUser;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.API.Controllers
{
    public class AuthController : BaseController
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserCommand command)
        {
            var result = await Mediator.Send(command);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(result.Data);
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserCommand command)
        {
            var result = await Mediator.Send(command);
            
            if (!result.Succeeded)
                return BadRequest(result.Errors);
                
            return Ok(result.Data);
        }
    }
} 