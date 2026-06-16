using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class CourtRepository : ICourtRepository
    {
        private readonly ApplicationDbContext _context;

        public CourtRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Court?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _context.Courts
                .FirstOrDefaultAsync(court => court.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Court>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _context.Courts
                .AsNoTracking()
                .OrderBy(court => court.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Court court,
            CancellationToken cancellationToken)
        {
            await _context.Courts.AddAsync(court, cancellationToken);
        }

        public void Update(Court court)
        {
            _context.Courts.Update(court);
        }
    }
}