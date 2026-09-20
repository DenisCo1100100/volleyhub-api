using VolleyHub.Application.GameParticipants.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Common
{
    public sealed record GameDetailsDto(
        Guid Id,
        GameCourtSummaryDto Court,
        GameOrganizerSummaryDto Organizer,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int MaxPlayers,
        decimal PricePerPlayer,
        GameLevel RequiredLevel,
        GameJoinPolicy JoinPolicy,
        string? Description,
        GameStatus Status,
        int ApprovedParticipantCount,
        int PendingParticipantCount,
        int AvailableSpots,
        GameParticipantJoinStatus? CurrentUserJoinStatus,
        IReadOnlyList<GameParticipantSummaryDto> Participants,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt)
    {
        public Guid? RecurrenceId { get; init; }
        public int? OccurrenceNumber { get; init; }
        public int WaitlistedParticipantCount => Participants.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Waitlisted);
    }
}
