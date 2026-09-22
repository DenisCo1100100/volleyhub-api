using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IGameTemplateRepository
    {
        Task<GameTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<GameTemplate>> GetByOrganizerIdAsync(Guid organizerId, CancellationToken cancellationToken);
        Task AddAsync(GameTemplate template, CancellationToken cancellationToken);
        void Update(GameTemplate template);
        void Delete(GameTemplate template);
    }
}
