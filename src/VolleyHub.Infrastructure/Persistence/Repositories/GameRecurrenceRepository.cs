using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class GameRecurrenceRepository : IGameRecurrenceRepository
    {
        private readonly ApplicationDbContext _context;

        public GameRecurrenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GameRecurrence?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.GameRecurrences.SingleOrDefaultAsync(recurrence => recurrence.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Game>> GetOccurrencesAsync(Guid recurrenceId, CancellationToken cancellationToken)
        {
            return await _context.Games.Where(game => game.RecurrenceId == recurrenceId)
                .OrderBy(game => game.OccurrenceNumber).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(GameRecurrence recurrence, CancellationToken cancellationToken)
        {
            await _context.GameRecurrences.AddAsync(recurrence, cancellationToken);
        }

        public void Update(GameRecurrence recurrence)
        {
            _context.GameRecurrences.Update(recurrence);
        }
    }
}
