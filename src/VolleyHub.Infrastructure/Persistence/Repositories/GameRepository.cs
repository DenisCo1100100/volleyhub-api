using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class GameRepository : IGameRepository
    {
        private readonly ApplicationDbContext _context;

        public GameRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Game?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _context.Games
                .FirstOrDefaultAsync(game => game.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Game>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _context.Games
                .AsNoTracking()
                .OrderBy(game => game.StartsAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Game game,
            CancellationToken cancellationToken)
        {
            await _context.Games.AddAsync(game, cancellationToken);
        }

        public void Update(Game game)
        {
            _context.Games.Update(game);
        }
    }
}