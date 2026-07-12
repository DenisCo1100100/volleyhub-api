using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Dtos
{
    public sealed record GameDto(
        Guid Id,
        Guid OrganizerId,
        Guid CourtId,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int MaxPlayers,
        decimal PricePerPlayer,
        GameLevel RequiredLevel,
        GameJoinPolicy JoinPolicy,
        string? Description,
        GameStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}