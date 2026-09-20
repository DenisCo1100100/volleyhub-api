using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IGameRepository
    {
        Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<PagedResult<GameSummaryDto>> GetSummariesAsync(GameSummaryQueryParameters parameters, CancellationToken cancellationToken);
        Task<PagedResult<PlayerGameHistoryDto>> GetPlayerHistoryAsync(GameHistoryQueryParameters parameters, CancellationToken cancellationToken);
        Task<PagedResult<OrganizedGameHistoryDto>> GetOrganizedHistoryAsync(GameHistoryQueryParameters parameters, CancellationToken cancellationToken);
        Task AddAsync(Game game, CancellationToken cancellationToken);
        void Update(Game game);
    }
}
