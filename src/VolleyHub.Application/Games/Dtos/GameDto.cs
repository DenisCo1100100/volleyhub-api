using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Dtos
{
    public sealed record GameDto(
        Guid Id,
        Guid CourtId,
        DateTimeOffset StartsAt,
        int MaxPlayers,
        string? Description,
        GameStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}