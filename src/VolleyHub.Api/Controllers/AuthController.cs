using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.Auth.Commands.LoginUser;
using VolleyHub.Application.Auth.Commands.RegisterUser;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterUserCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginUserCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return Ok(result);
        }
    }
}