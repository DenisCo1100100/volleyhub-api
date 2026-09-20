using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IGameRecurrenceRepository
    {
        Task<GameRecurrence?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Game>> GetOccurrencesAsync(Guid recurrenceId, CancellationToken cancellationToken);
        Task AddAsync(GameRecurrence recurrence, CancellationToken cancellationToken);
        void Update(GameRecurrence recurrence);
    }
}
