using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
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

        public async Task<PlayerReliabilitySummaryDto> GetReliabilitySummaryAsync(Guid playerProfileId, CancellationToken cancellationToken)
        {
            return await BuildReliabilitySummaryQuery(playerProfileId).SingleOrDefaultAsync(cancellationToken)
                ?? new PlayerReliabilitySummaryDto(playerProfileId, 0, 0, 0, 0, 0);
        }

        internal IQueryable<PlayerReliabilitySummaryDto> BuildReliabilitySummaryQuery(Guid playerProfileId)
        {
            // Completion is explicit; elapsed time alone never establishes attendance or a no-show.
            var participants = from participant in _context.GameParticipants.AsNoTracking()
                               join game in _context.Games.AsNoTracking() on participant.GameId equals game.Id
                               where participant.PlayerProfileId == playerProfileId && game.Status == GameStatus.Completed
                               select participant;

            return participants.GroupBy(participant => participant.PlayerProfileId)
                .Select(group => new PlayerReliabilitySummaryDto(
                    group.Key,
                    group.Count(p => p.JoinStatus == GameParticipantJoinStatus.Approved && p.AttendanceStatus == GameParticipantAttendanceStatus.Present),
                    group.Count(p => p.JoinStatus == GameParticipantJoinStatus.Approved && p.AttendanceStatus == GameParticipantAttendanceStatus.Absent),
                    group.Count(p => p.JoinStatus == GameParticipantJoinStatus.Cancelled && p.ApprovedAt != null && p.CancelledAt != null
                        && p.CancellationType == GameParticipantCancellationType.Late),
                    group.Count(p => p.JoinStatus == GameParticipantJoinStatus.Cancelled && p.ApprovedAt != null && p.CancelledAt != null
                        && p.CancellationType == GameParticipantCancellationType.OnTime),
                    group.Count(p => p.JoinStatus == GameParticipantJoinStatus.Approved && p.AttendanceStatus == GameParticipantAttendanceStatus.NotMarked)));
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
