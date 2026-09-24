using VolleyHub.Domain.Games;

using VolleyHub.Application.PlayerProfiles.Dtos;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IGameParticipantRepository
    {
        Task<GameParticipant?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<GameParticipant?> GetByGameAndPlayerProfileIdAsync(
            Guid gameId,
            Guid playerProfileId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<GameParticipant>> GetByGameIdAsync(
            Guid gameId,
            CancellationToken cancellationToken);

        Task<PlayerReliabilitySummaryDto> GetReliabilitySummaryAsync(Guid playerProfileId, CancellationToken cancellationToken);

        Task AddAsync(
            GameParticipant participant,
            CancellationToken cancellationToken);

        void Update(GameParticipant participant);
    }
}
