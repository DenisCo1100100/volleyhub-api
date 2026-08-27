using VolleyHub.Domain.Auth;

namespace VolleyHub.Application.Common.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
        Task<int> RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
        void Update(RefreshToken refreshToken);
    }
}