using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Dtos
{
    public sealed record GameParticipantDto(
        Guid Id,
        Guid GameId,
        Guid PlayerProfileId,
        DateTimeOffset JoinedAt,
        DateTimeOffset? ApprovedAt,
        GameParticipantJoinStatus JoinStatus,
        GameParticipantAttendanceStatus AttendanceStatus,
        GameParticipantOfflinePaymentStatus OfflinePaymentStatus,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}