using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Persistence.Repositories;
using VolleyHub.Infrastructure.Services;

namespace VolleyHub.Api.IntegrationTests.Infrastructure
{
    public sealed class GameHistoryQueryTests
    {
        [Theory]
        [InlineData(GameHistoryPeriod.All)]
        [InlineData(GameHistoryPeriod.Upcoming)]
        [InlineData(GameHistoryPeriod.Past)]
        public void History_ShouldGenerateScopedPagedPostgreSqlProjections(GameHistoryPeriod period)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres").Options;
            using var context = new ApplicationDbContext(options, new DateTimeProvider());
            var repository = new GameRepository(context);
            var now = DateTimeOffset.UtcNow;
            var parameters = new GameHistoryQueryParameters(Guid.NewGuid(), now, 2, 10, period, GameStatus.Open,
                now.AddDays(-7), now.AddDays(7), Guid.NewGuid(), GameParticipantJoinStatus.Approved);

            var playerSql = repository.BuildPlayerHistoryQuery(parameters).ToQueryString();
            var organizerSql = repository.BuildOrganizedHistoryQuery(parameters).ToQueryString();

            foreach (var sql in new[] { playerSql, organizerSql })
            {
                sql.Should().Contain("FROM games AS");
                sql.Should().Contain("JOIN courts AS");
                sql.Should().Contain("JOIN player_profiles AS");
                sql.Should().Contain("FROM game_participants AS");
                sql.Should().Contain("count(*)");
                sql.Should().Contain("LIMIT").And.Contain("OFFSET").And.Contain("ORDER BY");
                sql.Should().Contain("starts_at >=").And.Contain("starts_at <=");
                sql.Should().Contain("court_id =").And.Contain("status = @parameters_Status_Value");
                sql.Should().NotContain("is_deleted");
                sql.Should().NotContain("password_hash").And.NotContain("version").And.NotContain("created_at");
                sql.Should().Contain(period == GameHistoryPeriod.Upcoming ? "ORDER BY g.starts_at, g.id" : "ORDER BY g.starts_at DESC, g.id DESC");
            }
            playerSql.Should().Contain("EXISTS").And.Contain("player_profile_id =");
            playerSql.Should().Contain("cancelled_at").And.Contain("removed_at").And.Contain("cancellation_type");
            playerSql.Should().Contain("attendance_status").And.Contain("offline_payment_status");
            organizerSql.Should().Contain("organizer_id =");
            organizerSql.Should().Contain("Waitlisted").And.Contain("Present").And.Contain("Absent").And.Contain("NotMarked").And.Contain("Paid");
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        [Fact]
        public async Task History_ShouldUseClockBoundaryWithoutInferringCompletionAndShouldNotTrackEntities()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"history-boundary-{Guid.NewGuid()}").Options;
            await using var context = new ApplicationDbContext(options, new DateTimeProvider());
            var now = DateTimeOffset.UtcNow;
            var organizer = Domain.PlayerProfiles.PlayerProfile.Create(Guid.NewGuid(), "Organizer", Domain.PlayerProfiles.PlayerSkillLevel.Intermediate, null, null);
            var court = Domain.Courts.Court.Create(organizer.Id, "Court", "Address", 53.9, 27.56, Domain.Courts.CourtSurfaceType.Indoor, true, null);
            context.AddRange(organizer, court);
            var games = new[] { now.AddSeconds(-1), now, now.AddSeconds(1) }
                .Select(start => Game.Create(organizer.Id, court.Id, start, null, 12, 0, GameLevel.Any, GameJoinPolicy.Open, null)).ToArray();
            context.Games.AddRange(games);
            foreach (var game in games)
            {
                context.GameParticipants.Add(GameParticipant.JoinOpenGame(game.Id, organizer.Id, now.AddDays(-1), GameParticipantOfflinePaymentStatus.NotRequired));
            }
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var repository = new GameRepository(context);
            var parameters = new GameHistoryQueryParameters(organizer.Id, now, 1, 20, GameHistoryPeriod.Upcoming, null, null, null, null);

            (await repository.GetPlayerHistoryAsync(parameters, CancellationToken.None)).Items.Select(item => item.Game.Id).Should().Equal(games[2].Id);
            var upcoming = await repository.GetOrganizedHistoryAsync(parameters, CancellationToken.None);
            upcoming.Items.Select(item => item.Game.Id).Should().Equal(games[2].Id);
            upcoming.Items.Single().OfflinePayments.ExpectedParticipantCount.Should().Be(0);
            upcoming.Items.Single().OfflinePayments.OutstandingParticipantCount.Should().Be(0);
            var past = await repository.GetPlayerHistoryAsync(parameters with { Period = GameHistoryPeriod.Past }, CancellationToken.None);
            past.Items.Select(item => item.Game.Id).Should().Equal(games[1].Id, games[0].Id);
            past.Items.Should().OnlyContain(item => item.Game.Status == GameStatus.Open);
            (await repository.GetOrganizedHistoryAsync(parameters with { Period = GameHistoryPeriod.Past }, CancellationToken.None))
                .Items.Select(item => item.Game.Id).Should().Equal(games[1].Id, games[0].Id);
            context.ChangeTracker.Entries().Should().BeEmpty();
        }
    }
}
