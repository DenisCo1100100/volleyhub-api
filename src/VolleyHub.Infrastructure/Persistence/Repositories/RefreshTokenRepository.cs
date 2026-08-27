using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Auth;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(refreshToken => refreshToken.Id == id, cancellationToken);
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<int> RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            var expiredRefreshTokens = await _context.RefreshTokens
                .Where(refreshToken => refreshToken.ExpiresAt <= now)
                .ToListAsync(cancellationToken);

            if (expiredRefreshTokens.Count == 0)
            {
                return 0;
            }

            _context.RefreshTokens.RemoveRange(expiredRefreshTokens);

            return expiredRefreshTokens.Count;
        }

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public void Update(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
        }
    }
}