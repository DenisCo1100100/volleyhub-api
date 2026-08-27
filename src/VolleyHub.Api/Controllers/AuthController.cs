using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VolleyHub.Api.Contracts.Auth;
using VolleyHub.Api.Options;
using VolleyHub.Application.Auth.Commands.LoginUser;
using VolleyHub.Application.Auth.Commands.Logout;
using VolleyHub.Application.Auth.Commands.RefreshSession;
using VolleyHub.Application.Auth.Commands.RegisterUser;
using VolleyHub.Application.Auth.Dtos;
using VolleyHub.Application.Common.Exceptions;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly RefreshTokenCookieOptions _cookieOptions;

        public AuthController(ISender sender, IOptions<RefreshTokenCookieOptions> cookieOptions)
        {
            _sender = sender;
            _cookieOptions = cookieOptions.Value;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            SetRefreshTokenCookie(result);

            return Ok(ToResponse(result));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            SetRefreshTokenCookie(result);

            return Ok(ToResponse(result));
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
        {
            var refreshToken = GetRequiredRefreshTokenCookie();

            var result = await _sender.Send(
                new RefreshSessionCommand(refreshToken),
                cancellationToken);

            SetRefreshTokenCookie(result);

            return Ok(ToResponse(result));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            Request.Cookies.TryGetValue(_cookieOptions.Name, out var refreshToken);

            await _sender.Send(
                new LogoutCommand(refreshToken),
                cancellationToken);

            DeleteRefreshTokenCookie();

            return NoContent();
        }

        private string GetRequiredRefreshTokenCookie()
        {
            if (!Request.Cookies.TryGetValue(_cookieOptions.Name, out var refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedException("Refresh token cookie is missing.");
            }

            return refreshToken;
        }

        private void SetRefreshTokenCookie(AuthResultDto result)
        {
            Response.Cookies.Append(
                _cookieOptions.Name,
                result.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _cookieOptions.Secure,
                    SameSite = _cookieOptions.SameSite,
                    Expires = result.RefreshTokenExpiresAt,
                    Path = "/api/auth"
                });
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(
                _cookieOptions.Name,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = _cookieOptions.Secure,
                    SameSite = _cookieOptions.SameSite,
                    Path = "/api/auth"
                });
        }

        private static AuthResponse ToResponse(AuthResultDto result)
        {
            return new AuthResponse(
                result.UserId,
                result.Email,
                result.AccessToken);
        }
    }
}