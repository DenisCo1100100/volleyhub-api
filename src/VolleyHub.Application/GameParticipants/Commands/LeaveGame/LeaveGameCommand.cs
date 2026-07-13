using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.LeaveGame
{
    public sealed record LeaveGameCommand(
        Guid GameId,
        Guid PlayerProfileId) : IRequest;
}