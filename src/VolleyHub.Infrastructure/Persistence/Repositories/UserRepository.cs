using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Users;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return await _context.Users
                .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
        }

        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }
    }
}