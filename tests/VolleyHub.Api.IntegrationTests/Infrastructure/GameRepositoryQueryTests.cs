using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Persistence.Repositories;
using VolleyHub.Infrastructure.Services;

namespace VolleyHub.Api.IntegrationTests.Infrastructure
{
    public sealed class GameRepositoryQueryTests
    {
        [Fact]
        public void BuildSummariesQuery_ShouldGeneratePagedPostgreSqlProjection()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres")
                .Options;

            using var context = new ApplicationDbContext(
                options,
                new DateTimeProvider());

            var repository = new GameRepository(context);

            var parameters = new GameSummaryQueryParameters(
                Page: 2,
                PageSize: 10,
                StartsAtFrom: DateTimeOffset.UtcNow.AddDays(1),
                StartsAtTo: DateTimeOffset.UtcNow.AddDays(7),
                CourtId: Guid.NewGuid(),
                Status: GameStatus.Open,
                CurrentUserId: Guid.NewGuid());

            var sql = repository
                .BuildSummariesQuery(parameters)
                .ToQueryString();

            sql.Should().Contain("FROM games AS");
            sql.Should().Contain("FROM game_participants AS");
            sql.Should().Contain("FROM courts AS");
            sql.Should().Contain("FROM player_profiles AS");
            sql.Should().Contain("INNER JOIN");
            sql.Should().Contain("join_status");
            sql.Should().Contain("user_id");
            sql.Should().Contain("ORDER BY g.starts_at, g.id");
            sql.Should().Contain("LIMIT");
            sql.Should().Contain("OFFSET");
            sql.Should().Contain("count(*)");
            sql.Should().Contain("Approved");
            sql.Should().Contain("PendingApproval");
        }

        [Fact]
        public void BuildSummariesQuery_ShouldGeneratePostgreSqlWithoutCurrentUserLookup_WhenAnonymous()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres")
                .Options;

            using var context = new ApplicationDbContext(
                options,
                new DateTimeProvider());

            var repository = new GameRepository(context);

            var parameters = new GameSummaryQueryParameters(
                Page: 1,
                PageSize: 20,
                StartsAtFrom: null,
                StartsAtTo: null,
                CourtId: null,
                Status: null,
                CurrentUserId: null);

            var sql = repository
                .BuildSummariesQuery(parameters)
                .ToQueryString();

            sql.Should().Contain("FROM games AS");
            sql.Should().Contain("FROM game_participants AS");
            sql.Should().Contain("FROM courts AS");
            sql.Should().Contain("FROM player_profiles AS");
            sql.Should().Contain("INNER JOIN");
            sql.Should().Contain("ORDER BY g.starts_at, g.id");
            sql.Should().Contain("LIMIT");
            sql.Should().Contain("count(*)");

            sql.Should().NotContain("user_id");
        }
    }
}