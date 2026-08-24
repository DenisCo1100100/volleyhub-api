using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
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

        public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Games
                .FirstOrDefaultAsync(game => game.Id == id, cancellationToken);
        }

        public async Task<PagedResult<GameSummaryDto>> GetSummariesAsync(GameSummaryQueryParameters parameters, CancellationToken cancellationToken)
        {
            var filteredGames = BuildFilteredGamesQuery(parameters);

            var totalCount = await filteredGames
                .CountAsync(cancellationToken);

            var items = await BuildSummariesQuery(parameters)
                .ToListAsync(cancellationToken);

            return new PagedResult<GameSummaryDto>(
                items,
                parameters.Page,
                parameters.PageSize,
                totalCount);
        }

        internal IQueryable<GameSummaryDto> BuildSummariesQuery(GameSummaryQueryParameters parameters)
        {
            var filteredGames = BuildFilteredGamesQuery(parameters);

            var query =
                from game in filteredGames
                join court in _context.Courts.AsNoTracking() on game.CourtId equals court.Id
                join organizer in _context.PlayerProfiles.AsNoTracking() on game.OrganizerId equals organizer.Id
                let approvedParticipantCount = _context.GameParticipants.Count(
                    participant => participant.GameId == game.Id
                        && participant.JoinStatus == GameParticipantJoinStatus.Approved)
                let pendingParticipantCount = _context.GameParticipants.Count(
                    participant => participant.GameId == game.Id
                        && participant.JoinStatus == GameParticipantJoinStatus.PendingApproval)
                let currentUserJoinStatus = parameters.CurrentUserId == null
                    ? null
                    : (
                        from participant in _context.GameParticipants
                        join playerProfile in _context.PlayerProfiles on participant.PlayerProfileId equals playerProfile.Id
                        where participant.GameId == game.Id
                            && playerProfile.UserId == parameters.CurrentUserId.Value
                        select (GameParticipantJoinStatus?)participant.JoinStatus
                    ).FirstOrDefault()
                orderby game.StartsAt, game.Id
                select new GameSummaryDto(
                    game.Id,
                    new GameCourtSummaryDto(
                        court.Id,
                        court.Name,
                        court.Address,
                        court.Latitude,
                        court.Longitude,
                        court.SurfaceType,
                        court.IsIndoor),
                    new GameOrganizerSummaryDto(
                        organizer.Id,
                        organizer.DisplayName,
                        organizer.SkillLevel),
                    game.StartsAt,
                    game.EndsAt,
                    game.MaxPlayers,
                    game.PricePerPlayer,
                    game.RequiredLevel,
                    game.JoinPolicy,
                    game.Status,
                    approvedParticipantCount,
                    pendingParticipantCount,
                    game.MaxPlayers > approvedParticipantCount
                        ? game.MaxPlayers - approvedParticipantCount
                        : 0,
                    currentUserJoinStatus);

            return query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize);
        }

        public async Task AddAsync(Game game, CancellationToken cancellationToken)
        {
            await _context.Games.AddAsync(game, cancellationToken);
        }

        public void Update(Game game)
        {
            _context.Games.Update(game);
        }

        private IQueryable<Game> BuildFilteredGamesQuery(GameSummaryQueryParameters parameters)
        {
            var query = _context.Games.AsNoTracking();

            if (parameters.StartsAtFrom is not null)
            {
                query = query.Where(game => game.StartsAt >= parameters.StartsAtFrom.Value);
            }

            if (parameters.StartsAtTo is not null)
            {
                query = query.Where(game => game.StartsAt <= parameters.StartsAtTo.Value);
            }

            if (parameters.CourtId is not null)
            {
                query = query.Where(game => game.CourtId == parameters.CourtId.Value);
            }

            if (parameters.Status is not null)
            {
                query = query.Where(game => game.Status == parameters.Status.Value);
            }

            return query;
        }
    }
}