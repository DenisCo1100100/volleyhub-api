using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class PlayerProfileRepository : IPlayerProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public PlayerProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlayerProfile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _context.PlayerProfiles
                .FirstOrDefaultAsync(playerProfile => playerProfile.Id == id, cancellationToken);
        }

        public async Task<PlayerProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await _context.PlayerProfiles
                .FirstOrDefaultAsync(playerProfile => playerProfile.UserId == userId, cancellationToken);
        }

        public async Task<IReadOnlyList<PlayerProfile>> GetListAsync(
            CancellationToken cancellationToken)
        {
            return await _context.PlayerProfiles
                .AsNoTracking()
                .OrderBy(playerProfile => playerProfile.DisplayName)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            PlayerProfile playerProfile,
            CancellationToken cancellationToken)
        {
            await _context.PlayerProfiles.AddAsync(playerProfile, cancellationToken);
        }

        public void Update(PlayerProfile playerProfile)
        {
            _context.PlayerProfiles.Update(playerProfile);
        }
    }
}