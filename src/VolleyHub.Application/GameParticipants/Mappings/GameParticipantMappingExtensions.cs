using VolleyHub.Application.GameParticipants.Dtos;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Mappings
{
    public static class GameParticipantMappingExtensions
    {
        public static GameParticipantDto ToDto(this GameParticipant participant)
        {
            return new GameParticipantDto(
                participant.Id,
                participant.GameId,
                participant.PlayerProfileId,
                participant.JoinedAt,
                participant.ApprovedAt,
                participant.JoinStatus,
                participant.AttendanceStatus,
                participant.OfflinePaymentStatus,
                participant.CreatedAt,
                participant.UpdatedAt);
        }
    }
}