using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface ICourtRepository
    {
        Task<Court?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<Court>> GetListAsync(CancellationToken cancellationToken);
        Task AddAsync(Court court, CancellationToken cancellationToken);
        void Update(Court court);
    }
}