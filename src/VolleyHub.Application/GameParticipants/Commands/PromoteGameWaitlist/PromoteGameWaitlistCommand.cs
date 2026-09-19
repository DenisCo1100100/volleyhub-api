using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.PromoteGameWaitlist
{
    public sealed record PromoteGameWaitlistCommand(Guid GameId) : IRequest<Guid>;
}
