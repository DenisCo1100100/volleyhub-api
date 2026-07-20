using VolleyHub.Application.Games.Dtos;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Mappings
{
    public static class GameMappingExtensions
    {
        public static GameDto ToDto(
            this Game game,
            IReadOnlyList<GameParticipant>? participants = null,
            Guid? currentPlayerProfileId = null)
        {
            participants ??= [];

            var approvedParticipantCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);

            var pendingParticipantCount = participants.Count(
                participant => participant.JoinStatus is GameParticipantJoinStatus.PendingApproval);

            var currentUserJoinStatus = currentPlayerProfileId is null
                ? null
                : participants
                    .FirstOrDefault(participant => participant.PlayerProfileId == currentPlayerProfileId.Value)
                    ?.JoinStatus;

            return new GameDto(
                game.Id,
                game.OrganizerId,
                game.CourtId,
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
                game.CreatedAt,
                game.UpdatedAt);
        }
    }
}