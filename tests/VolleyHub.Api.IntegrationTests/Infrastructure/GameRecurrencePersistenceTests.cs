using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Domain.Games;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Services;

namespace VolleyHub.Api.IntegrationTests.Infrastructure
{
    public sealed class GameRecurrencePersistenceTests
    {
        [Fact]
        public void PostgreSqlModel_ShouldProtectOccurrenceIdentityAndMatchMigration()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres").Options, new DateTimeProvider());
            var game = context.Model.FindEntityType(typeof(Game))!;
            game.GetIndexes().Single(index => index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Game.RecurrenceId), nameof(Game.OccurrenceNumber) })).IsUnique.Should().BeTrue();
            context.Model.FindEntityType(typeof(GameRecurrence))!.FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
            context.Database.HasPendingModelChanges().Should().BeFalse();
            var sql = context.Database.GenerateCreateScript();
            sql.Should().Contain("CREATE UNIQUE INDEX \"IX_games_recurrence_id_occurrence_number\"");
            sql.Should().Contain("CK_games_recurrence_occurrence").And.Contain("CK_game_recurrences_occurrence_count");
            sql.Should().Contain("FK_games_game_recurrences_recurrence_id");
            sql.Should().Contain("ON DELETE RESTRICT");
        }

        [Fact]
        public async Task ConcurrentSeriesChanges_ShouldRejectStaleDefinition()
        {
            var options = Options();
            var recurrenceId = await SeedAsync(options);
            await using var first = new ApplicationDbContext(options, new DateTimeProvider());
            await using var second = new ApplicationDbContext(options, new DateTimeProvider());
            var firstRecurrence = await first.GameRecurrences.FindAsync(recurrenceId);
            var secondRecurrence = await second.GameRecurrences.FindAsync(recurrenceId);
            firstRecurrence!.Cancel(DateTimeOffset.UtcNow);
            secondRecurrence!.Cancel(DateTimeOffset.UtcNow);
            await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);
            var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task SeriesCancellation_ShouldConflictWithConcurrentParticipation()
        {
            var options = Options();
            var recurrenceId = await SeedAsync(options);
            await using var cancellation = new ApplicationDbContext(options, new DateTimeProvider());
            var game = await cancellation.Games.SingleAsync(game => game.RecurrenceId == recurrenceId && game.OccurrenceNumber == 1);
            await using (var joining = new ApplicationDbContext(options, new DateTimeProvider()))
            {
                joining.GameParticipants.Add(GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, GameParticipantOfflinePaymentStatus.Pending));
                await joining.SaveChangesAsync();
            }
            game.Cancel();
            var save = () => new UnitOfWork(cancellation).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task RecurrenceAndOccurrences_ShouldRoundTripWithNullableDurationAndStableIdentity()
        {
            var options = Options();
            var id = await SeedAsync(options);
            await using var context = new ApplicationDbContext(options, new DateTimeProvider());
            var recurrence = await context.GameRecurrences.SingleAsync(r => r.Id == id);
            var games = await context.Games.Where(g => g.RecurrenceId == id).OrderBy(g => g.OccurrenceNumber).ToListAsync();
            recurrence.Duration.Should().BeNull();
            games.Select(g => g.OccurrenceNumber).Should().Equal(1, 2);
            games[1].StartsAt.Should().Be(games[0].StartsAt.AddDays(7));
            recurrence.CreatedAt.Should().NotBe(default);
        }

        private static DbContextOptions<ApplicationDbContext> Options() => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"recurrence-{Guid.NewGuid()}").Options;

        private static async Task<Guid> SeedAsync(DbContextOptions<ApplicationDbContext> options)
        {
            await using var context = new ApplicationDbContext(options, new DateTimeProvider());
            var now = DateTimeOffset.UtcNow;
            var source = Game.Create(Guid.NewGuid(), Guid.NewGuid(), now.AddDays(1), null, 12, 15, GameLevel.Any, GameJoinPolicy.Open, null);
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), source, now.AddDays(8), 2, now);
            context.Games.Add(source);
            context.GameRecurrences.Add(recurrence);
            context.Games.AddRange(recurrence.CreateOccurrence(1), recurrence.CreateOccurrence(2));
            await context.SaveChangesAsync();
            return recurrence.Id;
        }
    }
}
