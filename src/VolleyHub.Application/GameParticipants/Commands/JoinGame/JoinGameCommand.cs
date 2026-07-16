using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.JoinGame
{
    public sealed record JoinGameCommand(Guid GameId) : IRequest<Guid>;
}