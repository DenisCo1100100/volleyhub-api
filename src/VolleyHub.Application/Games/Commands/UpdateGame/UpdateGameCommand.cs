using MediatR;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.UpdateGame
{
    public sealed record UpdateGameCommand(
        Guid Id,
        Guid OrganizerId,
        Guid CourtId,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int MaxPlayers,
        decimal PricePerPlayer,
        GameLevel RequiredLevel,
        GameJoinPolicy JoinPolicy,
        string? Description) : IRequest;
}