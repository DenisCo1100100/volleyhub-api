using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Common
{
    public sealed record PlayerGameHistoryDto(GameSummaryDto Game, GameParticipationHistoryDto Participation);

    public sealed record GameParticipationHistoryDto(
        Guid Id,
        GameParticipantJoinStatus JoinStatus,
        GameParticipantAttendanceStatus AttendanceStatus,
        GameParticipantOfflinePaymentStatus OfflinePaymentStatus,
        DateTimeOffset JoinedAt,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset? RemovedAt,
        GameParticipantCancellationType? CancellationType);
}
