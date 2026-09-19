using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.JoinGameWaitlist
{
    public sealed record JoinGameWaitlistCommand(Guid GameId) : IRequest<Guid>;
}
