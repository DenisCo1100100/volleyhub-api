using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Common
{
    public sealed record GameParticipantSummaryDto(
        Guid Id,
        Guid PlayerProfileId,
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        GameParticipantJoinStatus JoinStatus,
        GameParticipantAttendanceStatus AttendanceStatus,
        GameParticipantOfflinePaymentStatus OfflinePaymentStatus,
        DateTimeOffset JoinedAt,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset? RemovedAt,
        GameParticipantCancellationType? CancellationType);
}