using MediatR;
using VolleyHub.Application.Auth.Dtos;

namespace VolleyHub.Application.Auth.Commands.RegisterUser
{
    public sealed record RegisterUserCommand(
        string Email,
        string Password) : IRequest<AuthResultDto>;
}