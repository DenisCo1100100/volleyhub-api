using VolleyHub.Domain.Games;

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

        Task<IReadOnlyList<GameParticipant>> GetByPlayerProfileIdAsync(
            Guid playerProfileId,
            CancellationToken cancellationToken);

        Task AddAsync(
            GameParticipant participant,
            CancellationToken cancellationToken);

        void Update(GameParticipant participant);
    }
}