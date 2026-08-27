using MediatR;

namespace VolleyHub.Application.Auth.Commands.Logout
{
    public sealed record LogoutCommand(string? RefreshToken) : IRequest;
}