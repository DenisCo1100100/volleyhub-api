using MediatR;

namespace VolleyHub.Application.Games.Commands.CreateGame
{
    public sealed record CreateGameCommand(
        Guid CourtId,
        DateTimeOffset StartsAt,
        int MaxPlayers,
        string? Description) : IRequest<Guid>;
}