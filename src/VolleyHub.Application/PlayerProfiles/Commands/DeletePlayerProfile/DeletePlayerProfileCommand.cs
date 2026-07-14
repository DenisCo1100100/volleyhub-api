using MediatR;

namespace VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile
{
    public sealed record DeletePlayerProfileCommand(Guid Id) : IRequest;
}