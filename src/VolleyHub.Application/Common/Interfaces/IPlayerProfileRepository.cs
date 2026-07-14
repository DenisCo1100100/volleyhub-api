using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IPlayerProfileRepository
    {
        Task<PlayerProfile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<PlayerProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<PlayerProfile>> GetListAsync(
            CancellationToken cancellationToken);

        Task AddAsync(
            PlayerProfile playerProfile,
            CancellationToken cancellationToken);

        void Update(PlayerProfile playerProfile);
    }
}