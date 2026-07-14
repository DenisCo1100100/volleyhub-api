using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed class GameParticipantRepository : IGameParticipantRepository
    {
        private readonly ApplicationDbContext _context;

        public GameParticipantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GameParticipant?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _context.GameParticipants
                .FirstOrDefaultAsync(participant => participant.Id == id, cancellationToken);
        }

        public async Task<GameParticipant?> GetByGameAndPlayerProfileIdAsync(
            Guid gameId,
            Guid playerProfileId,
            CancellationToken cancellationToken)
        {
            return await _context.GameParticipants
                .FirstOrDefaultAsync(
                    participant => participant.GameId == gameId
                        && participant.PlayerProfileId == playerProfileId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<GameParticipant>> GetByGameIdAsync(
            Guid gameId,
            CancellationToken cancellationToken)
        {
            return await _context.GameParticipants
                .AsNoTracking()
                .Where(participant => participant.GameId == gameId)
                .OrderBy(participant => participant.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<GameParticipant>> GetByPlayerProfileIdAsync(
            Guid playerProfileId,
            CancellationToken cancellationToken)
        {
            return await _context.GameParticipants
                .AsNoTracking()
                .Where(participant => participant.PlayerProfileId == playerProfileId)
                .OrderByDescending(participant => participant.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            GameParticipant participant,
            CancellationToken cancellationToken)
        {
            await _context.GameParticipants.AddAsync(participant, cancellationToken);
        }

        public void Update(GameParticipant participant)
        {
            _context.GameParticipants.Update(participant);
        }
    }
}