using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IGameRepository
    {
        Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Game>> GetListAsync(CancellationToken cancellationToken);
        Task AddAsync(Game game, CancellationToken cancellationToken);
        void Update(Game game);
    }
}