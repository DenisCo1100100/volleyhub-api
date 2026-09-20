using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameRecurrences.Common
{
    public sealed record GameRecurrenceDto(Guid Id, Guid SourceGameId, Guid OrganizerId, Guid CourtId,
        DateTimeOffset FirstStartsAt, TimeSpan? Duration, int OccurrenceCount, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description,
        DateTimeOffset? CancelledAt, IReadOnlyList<GameOccurrenceDto> Occurrences);

    public sealed record GameOccurrenceDto(Guid GameId, int OccurrenceNumber, DateTimeOffset ScheduledStartsAt,
        DateTimeOffset StartsAt, DateTimeOffset? EndsAt, GameStatus Status);
}
