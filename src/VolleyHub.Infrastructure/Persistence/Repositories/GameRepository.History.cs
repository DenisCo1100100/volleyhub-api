using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Infrastructure.Persistence.Repositories
{
    public sealed partial class GameRepository
    {
        public async Task<PagedResult<PlayerGameHistoryDto>> GetPlayerHistoryAsync(GameHistoryQueryParameters parameters, CancellationToken cancellationToken)
        {
            var totalCount = await BuildPlayerHistoryGamesQuery(parameters).CountAsync(cancellationToken);
            var items = await BuildPlayerHistoryQuery(parameters).ToListAsync(cancellationToken);
            return new PagedResult<PlayerGameHistoryDto>(items, parameters.Page, parameters.PageSize, totalCount);
        }

        public async Task<PagedResult<OrganizedGameHistoryDto>> GetOrganizedHistoryAsync(GameHistoryQueryParameters parameters, CancellationToken cancellationToken)
        {
            var totalCount = await BuildOrganizedHistoryGamesQuery(parameters).CountAsync(cancellationToken);
            var items = await BuildOrganizedHistoryQuery(parameters).ToListAsync(cancellationToken);
            return new PagedResult<OrganizedGameHistoryDto>(items, parameters.Page, parameters.PageSize, totalCount);
        }

        internal IQueryable<PlayerGameHistoryDto> BuildPlayerHistoryQuery(GameHistoryQueryParameters parameters)
        {
            var games = PageHistoryGames(BuildPlayerHistoryGamesQuery(parameters), parameters);

            // Historical relationships remain readable after a court or organizer profile is soft-deleted.
            return
                from game in games
                join court in _context.Courts.IgnoreQueryFilters().AsNoTracking() on game.CourtId equals court.Id
                join organizer in _context.PlayerProfiles.IgnoreQueryFilters().AsNoTracking() on game.OrganizerId equals organizer.Id
                join participant in _context.GameParticipants.AsNoTracking() on game.Id equals participant.GameId
                where participant.PlayerProfileId == parameters.PlayerProfileId
                let approvedCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Approved)
                let pendingCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.PendingApproval)
                orderby parameters.Period == GameHistoryPeriod.Upcoming ? game.StartsAt : default
                ascending, parameters.Period == GameHistoryPeriod.Upcoming ? game.Id : Guid.Empty ascending,
                    game.StartsAt descending, game.Id descending
                select new PlayerGameHistoryDto(
                    new GameSummaryDto(
                        game.Id,
                        new GameCourtSummaryDto(court.Id, court.Name, court.Address, court.Latitude, court.Longitude, court.SurfaceType, court.IsIndoor),
                        new GameOrganizerSummaryDto(organizer.Id, organizer.DisplayName, organizer.SkillLevel),
                        game.StartsAt, game.EndsAt, game.MaxPlayers, game.PricePerPlayer, game.RequiredLevel, game.JoinPolicy, game.Status,
                        approvedCount, pendingCount, game.MaxPlayers > approvedCount ? game.MaxPlayers - approvedCount : 0, participant.JoinStatus)
                    {
                        RecurrenceId = game.RecurrenceId,
                        OccurrenceNumber = game.OccurrenceNumber
                    },
                    new GameParticipationHistoryDto(
                        participant.Id, participant.JoinStatus, participant.AttendanceStatus, participant.OfflinePaymentStatus,
                        participant.JoinedAt, participant.ApprovedAt, participant.CancelledAt, participant.RemovedAt, participant.CancellationType));
        }

        internal IQueryable<OrganizedGameHistoryDto> BuildOrganizedHistoryQuery(GameHistoryQueryParameters parameters)
        {
            var games = PageHistoryGames(BuildOrganizedHistoryGamesQuery(parameters), parameters);

            return
                from game in games
                join court in _context.Courts.IgnoreQueryFilters().AsNoTracking() on game.CourtId equals court.Id
                join organizer in _context.PlayerProfiles.IgnoreQueryFilters().AsNoTracking() on game.OrganizerId equals organizer.Id
                let approvedCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Approved)
                let pendingCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.PendingApproval)
                let waitlistedCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Waitlisted)
                let presentCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Approved
                    && p.AttendanceStatus == GameParticipantAttendanceStatus.Present)
                let absentCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Approved
                    && p.AttendanceStatus == GameParticipantAttendanceStatus.Absent)
                let unmarkedCount = _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Approved
                    && p.AttendanceStatus == GameParticipantAttendanceStatus.NotMarked)
                let paymentRequired = game.PricePerPlayer > 0 && (game.Status == GameStatus.Open || game.Status == GameStatus.Full || game.Status == GameStatus.Completed)
                let paidCount = paymentRequired
                    ? _context.GameParticipants.Count(p => p.GameId == game.Id && p.JoinStatus == GameParticipantJoinStatus.Approved
                        && p.OfflinePaymentStatus == GameParticipantOfflinePaymentStatus.Paid)
                    : 0
                let currentUserJoinStatus = _context.GameParticipants
                    .Where(p => p.GameId == game.Id && p.PlayerProfileId == parameters.PlayerProfileId)
                    .Select(p => (GameParticipantJoinStatus?)p.JoinStatus).FirstOrDefault()
                orderby parameters.Period == GameHistoryPeriod.Upcoming ? game.StartsAt : default
                ascending, parameters.Period == GameHistoryPeriod.Upcoming ? game.Id : Guid.Empty ascending,
                    game.StartsAt descending, game.Id descending
                select new OrganizedGameHistoryDto(
                    new GameSummaryDto(
                        game.Id,
                        new GameCourtSummaryDto(court.Id, court.Name, court.Address, court.Latitude, court.Longitude, court.SurfaceType, court.IsIndoor),
                        new GameOrganizerSummaryDto(organizer.Id, organizer.DisplayName, organizer.SkillLevel),
                        game.StartsAt, game.EndsAt, game.MaxPlayers, game.PricePerPlayer, game.RequiredLevel, game.JoinPolicy, game.Status,
                        approvedCount, pendingCount, game.MaxPlayers > approvedCount ? game.MaxPlayers - approvedCount : 0, currentUserJoinStatus)
                    {
                        RecurrenceId = game.RecurrenceId,
                        OccurrenceNumber = game.OccurrenceNumber
                    },
                    waitlistedCount, presentCount, absentCount, unmarkedCount,
                    new GameOfflinePaymentSummaryDto(game.Id, game.PricePerPlayer,
                        paymentRequired ? approvedCount : 0, paidCount, paymentRequired ? approvedCount - paidCount : 0));
        }

        private IQueryable<Game> BuildPlayerHistoryGamesQuery(GameHistoryQueryParameters parameters)
        {
            return BuildHistoryGamesQuery(parameters).Where(game => _context.GameParticipants.Any(participant =>
                participant.GameId == game.Id && participant.PlayerProfileId == parameters.PlayerProfileId
                && (parameters.JoinStatus == null || participant.JoinStatus == parameters.JoinStatus)));
        }

        private IQueryable<Game> BuildOrganizedHistoryGamesQuery(GameHistoryQueryParameters parameters)
        {
            return BuildHistoryGamesQuery(parameters).Where(game => game.OrganizerId == parameters.PlayerProfileId);
        }

        private IQueryable<Game> BuildHistoryGamesQuery(GameHistoryQueryParameters parameters)
        {
            var query = _context.Games.AsNoTracking();
            if (parameters.Period == GameHistoryPeriod.Upcoming)
            {
                query = query.Where(game => game.StartsAt > parameters.Now
                    && (game.Status == GameStatus.Draft || game.Status == GameStatus.Open || game.Status == GameStatus.Full));
            }
            else if (parameters.Period == GameHistoryPeriod.Past)
            {
                query = query.Where(game => game.StartsAt <= parameters.Now);
            }

            if (parameters.Status is not null)
            {
                query = query.Where(game => game.Status == parameters.Status.Value);
            }

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

            return query;
        }

        private static IQueryable<Game> PageHistoryGames(IQueryable<Game> query, GameHistoryQueryParameters parameters)
        {
            var ordered = parameters.Period == GameHistoryPeriod.Upcoming
                ? query.OrderBy(game => game.StartsAt).ThenBy(game => game.Id)
                : query.OrderByDescending(game => game.StartsAt).ThenByDescending(game => game.Id);

            return ordered.Skip((parameters.Page - 1) * parameters.PageSize).Take(parameters.PageSize);
        }
    }
}
