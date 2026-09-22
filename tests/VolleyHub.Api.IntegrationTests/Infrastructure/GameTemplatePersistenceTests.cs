using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Domain.Games;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Services;

namespace VolleyHub.Api.IntegrationTests.Infrastructure
{
    public sealed class GameTemplatePersistenceTests
    {
        [Fact]
        public void PostgreSqlModelAndMigration_ShouldStorePrivateSettingsWithoutGameDependency()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres").Options, new DateTimeProvider());
            var template = context.Model.FindEntityType(typeof(GameTemplate))!;
            template.FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
            template.FindProperty(nameof(GameTemplate.Duration))!.GetColumnType().Should().Be("interval");
            template.GetForeignKeys().Should().HaveCount(2).And.OnlyContain(key => key.DeleteBehavior == DeleteBehavior.Restrict);
            context.Model.FindEntityType(typeof(Game))!.GetForeignKeys().Should().NotContain(key => key.PrincipalEntityType.ClrType == typeof(GameTemplate));
            context.Database.HasPendingModelChanges().Should().BeFalse();

            var migrations = context.Database.GetMigrations().ToArray();
            var sql = context.GetService<IMigrator>().GenerateScript(migrations[^2], migrations[^1]);
            sql.Should().Contain("CREATE TABLE game_templates").And.Contain("CK_game_templates_duration")
                .And.Contain("FK_game_templates_player_profiles_organizer_id").And.Contain("FK_game_templates_courts_court_id")
                .And.Contain("IX_game_templates_organizer_id").And.NotContain("ALTER TABLE games");
            context.GetService<IMigrator>().GenerateScript(migrations[^1], migrations[^2]).Should().Contain("DROP TABLE game_templates");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConcurrentUpdateOrDelete_ShouldRejectStaleTemplate(bool delete)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"template-{Guid.NewGuid()}").Options;
            Guid id;
            await using (var seed = new ApplicationDbContext(options, new DateTimeProvider()))
            {
                var template = GameTemplate.Create(Guid.NewGuid(), "Practice", Guid.NewGuid(), null, 12, 0, GameLevel.Any, GameJoinPolicy.Open, null);
                seed.GameTemplates.Add(template);
                await seed.SaveChangesAsync();
                id = template.Id;
            }

            await using var first = new ApplicationDbContext(options, new DateTimeProvider());
            await using var second = new ApplicationDbContext(options, new DateTimeProvider());
            var firstTemplate = (await first.GameTemplates.FindAsync(id))!;
            var secondTemplate = (await second.GameTemplates.FindAsync(id))!;
            firstTemplate.Update("New settings", firstTemplate.CourtId, TimeSpan.FromHours(1), 6, 10, GameLevel.Beginner, GameJoinPolicy.ApprovalRequired, null);
            await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);
            if (delete) second.GameTemplates.Remove(secondTemplate);
            else secondTemplate.Update("Stale settings", secondTemplate.CourtId, null, 12, 0, GameLevel.Any, GameJoinPolicy.Open, null);

            var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();
            await using var verify = new ApplicationDbContext(options, new DateTimeProvider());
            (await verify.GameTemplates.SingleAsync()).Name.Should().Be("New settings");
        }
    }
}
