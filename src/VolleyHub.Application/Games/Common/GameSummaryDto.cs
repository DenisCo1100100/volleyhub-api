using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Common
{
    public sealed record GameSummaryDto(
        Guid Id,
        GameCourtSummaryDto Court,
        GameOrganizerSummaryDto Organizer,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int MaxPlayers,
        decimal PricePerPlayer,
        GameLevel RequiredLevel,
        GameJoinPolicy JoinPolicy,
        GameStatus Status,
        int ApprovedParticipantCount,
        int PendingParticipantCount,
        int AvailableSpots,
        GameParticipantJoinStatus? CurrentUserJoinStatus)
    {
        public Guid? RecurrenceId { get; init; }
        public int? OccurrenceNumber { get; init; }
    }
}
