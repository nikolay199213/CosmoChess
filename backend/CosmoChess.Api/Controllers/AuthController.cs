using CosmoChess.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CosmoChess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserCommand command)
        {
            var userId = await mediator.Send(command);
            return Ok(new { UserId = userId });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        {
            var token = await mediator.Send(command);
            return Ok(new { Token = token });
        }
    }
}
