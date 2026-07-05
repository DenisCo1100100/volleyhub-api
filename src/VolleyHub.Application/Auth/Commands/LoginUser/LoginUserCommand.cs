using MediatR;
using VolleyHub.Application.Auth.Dtos;

namespace VolleyHub.Application.Auth.Commands.LoginUser
{
    public sealed record LoginUserCommand(
        string Email,
        string Password) : IRequest<AuthResultDto>;
}