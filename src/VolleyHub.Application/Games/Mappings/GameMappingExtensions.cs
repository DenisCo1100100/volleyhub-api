using VolleyHub.Application.GameParticipants.Common;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Mappings
{
    public static class GameMappingExtensions
    {
        public static GameSummaryDto ToSummaryDto(
            this Game game,
            Court court,
            PlayerProfile organizer,
            IReadOnlyList<GameParticipant> participants,
            Guid? currentPlayerProfileId)
        {
            var approvedParticipantCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);

            var pendingParticipantCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.PendingApproval);

            var currentUserJoinStatus = currentPlayerProfileId is null
                ? null
                : participants
                    .FirstOrDefault(participant => participant.PlayerProfileId == currentPlayerProfileId.Value)
                    ?.JoinStatus;

            return new GameSummaryDto(
                game.Id,
                ToCourtSummary(court),
                ToOrganizerSummary(organizer),
                game.StartsAt,
                game.EndsAt,
                game.MaxPlayers,
                game.PricePerPlayer,
                game.RequiredLevel,
                game.JoinPolicy,
                game.Status,
                approvedParticipantCount,
                pendingParticipantCount,
                Math.Max(0, game.MaxPlayers - approvedParticipantCount),
                currentUserJoinStatus);
        }

        public static GameDetailsDto ToDetailsDto(
            this Game game,
            Court court,
            PlayerProfile organizer,
            IReadOnlyList<GameParticipantSummaryDto> participants,
            Guid? currentPlayerProfileId)
        {
            var approvedParticipantCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);

            var pendingParticipantCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.PendingApproval);

            var currentUserJoinStatus = currentPlayerProfileId is null
                ? null
                : participants
                    .FirstOrDefault(participant => participant.PlayerProfileId == currentPlayerProfileId.Value)
                    ?.JoinStatus;

            return new GameDetailsDto(
                game.Id,
                ToCourtSummary(court),
                ToOrganizerSummary(organizer),
                game.StartsAt,
                game.EndsAt,
                game.MaxPlayers,
                game.PricePerPlayer,
                game.RequiredLevel,
                game.JoinPolicy,
                game.Description,
                game.Status,
                approvedParticipantCount,
                pendingParticipantCount,
                Math.Max(0, game.MaxPlayers - approvedParticipantCount),
                currentUserJoinStatus,
                participants,
                game.CreatedAt,
                game.UpdatedAt);
        }

        private static GameCourtSummaryDto ToCourtSummary(Court court)
        {
            return new GameCourtSummaryDto(
                court.Id,
                court.Name,
                court.Address,
                court.Latitude,
                court.Longitude,
                court.SurfaceType,
                court.IsIndoor);
        }

        private static GameOrganizerSummaryDto ToOrganizerSummary(PlayerProfile organizer)
        {
            return new GameOrganizerSummaryDto(
                organizer.Id,
                organizer.DisplayName,
                organizer.SkillLevel);
        }
    }
}