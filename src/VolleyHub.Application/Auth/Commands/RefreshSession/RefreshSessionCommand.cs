using MediatR;
using VolleyHub.Application.Auth.Dtos;

namespace VolleyHub.Application.Auth.Commands.RefreshSession
{
    public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<AuthResultDto>;
}