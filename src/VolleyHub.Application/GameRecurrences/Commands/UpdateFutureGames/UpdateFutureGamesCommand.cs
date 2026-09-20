using MediatR;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameRecurrences.Commands.UpdateFutureGames
{
    public sealed record UpdateFutureGamesCommand(Guid Id, int FromOccurrenceNumber, Guid CourtId, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description) : IRequest;
}
