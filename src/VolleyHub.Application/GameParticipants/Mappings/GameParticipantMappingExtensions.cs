using VolleyHub.Application.GameParticipants.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Mappings
{
    public static class GameParticipantMappingExtensions
    {
        public static GameParticipantSummaryDto ToSummaryDto(this GameParticipant participant, PlayerProfile playerProfile)
        {
            return new GameParticipantSummaryDto(
                participant.Id,
                participant.PlayerProfileId,
                playerProfile.DisplayName,
                playerProfile.SkillLevel,
                participant.JoinStatus,
                participant.AttendanceStatus,
                participant.OfflinePaymentStatus,
                participant.JoinedAt,
                participant.ApprovedAt);
        }
    }
}