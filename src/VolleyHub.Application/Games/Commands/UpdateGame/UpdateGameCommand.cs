using MediatR;

namespace VolleyHub.Application.Games.Commands.UpdateGame
{
    public sealed record UpdateGameCommand(
        Guid Id,
        Guid CourtId,
        DateTimeOffset StartsAt,
        int MaxPlayers,
        string? Description) : IRequest;
}