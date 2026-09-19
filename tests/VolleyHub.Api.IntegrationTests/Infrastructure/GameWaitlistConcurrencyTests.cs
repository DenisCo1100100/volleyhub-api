using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Domain.Games;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Services;

namespace VolleyHub.Api.IntegrationTests.Infrastructure
{
    public sealed class GameWaitlistConcurrencyTests
    {
        [Fact]
        public async Task ConcurrentPromotions_ShouldAllowOnlyOneSaveForLastPlace()
        {
            var options = CreateOptions();
            var (gameId, participantId) = await SeedAsync(options);
            await using var first = new ApplicationDbContext(options, new DateTimeProvider());
            await using var second = new ApplicationDbContext(options, new DateTimeProvider());
            var firstGame = await first.Games.SingleAsync(game => game.Id == gameId);
            var secondGame = await second.Games.SingleAsync(game => game.Id == gameId);
            var firstParticipant = await first.GameParticipants.SingleAsync(participant => participant.Id == participantId);
            var secondParticipant = await second.GameParticipants.SingleAsync(participant => participant.Id == participantId);
            var firstCount = await first.GameParticipants.CountAsync(participant => participant.GameId == gameId && participant.JoinStatus == GameParticipantJoinStatus.Approved);
            var secondCount = await second.GameParticipants.CountAsync(participant => participant.GameId == gameId && participant.JoinStatus == GameParticipantJoinStatus.Approved);

            firstParticipant.PromoteFromWaitlist(firstGame, firstCount, DateTimeOffset.UtcNow);
            secondParticipant.PromoteFromWaitlist(secondGame, secondCount, DateTimeOffset.UtcNow);
            await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);

            Func<Task> save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();

            await using var verification = new ApplicationDbContext(options, new DateTimeProvider());
            (await verification.Games.SingleAsync()).Status.Should().Be(GameStatus.Full);
            (await verification.GameParticipants.CountAsync(participant => participant.JoinStatus == GameParticipantJoinStatus.Approved)).Should().Be(2);
        }

        [Fact]
        public async Task ParticipationChanges_ShouldCompeteOnGameVersion_EvenWhenGameRemainsOpen()
        {
            var options = CreateOptions();
            var (gameId, _) = await SeedAsync(options, maxPlayers: 4);
            await using var first = new ApplicationDbContext(options, new DateTimeProvider());
            await using var second = new ApplicationDbContext(options, new DateTimeProvider());
            await first.Games.SingleAsync(game => game.Id == gameId);
            await second.Games.SingleAsync(game => game.Id == gameId);
            first.GameParticipants.Add(GameParticipant.JoinOpenGame(gameId, Guid.NewGuid(), DateTimeOffset.UtcNow, GameParticipantOfflinePaymentStatus.NotRequired));
            second.GameParticipants.Add(GameParticipant.JoinOpenGame(gameId, Guid.NewGuid(), DateTimeOffset.UtcNow, GameParticipantOfflinePaymentStatus.NotRequired));

            await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);
            Func<Task> save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task StaleWithdrawal_ShouldNotOverwritePromotion_WhenGameWasReadAfterPromotion()
        {
            var options = CreateOptions();
            var (gameId, participantId) = await SeedAsync(options);
            await using var withdrawal = new ApplicationDbContext(options, new DateTimeProvider());
            var stale = await withdrawal.GameParticipants.SingleAsync(participant => participant.Id == participantId);

            await using (var promotion = new ApplicationDbContext(options, new DateTimeProvider()))
            {
                var game = await promotion.Games.SingleAsync(game => game.Id == gameId);
                var participant = await promotion.GameParticipants.SingleAsync(participant => participant.Id == participantId);
                participant.PromoteFromWaitlist(game, 1, DateTimeOffset.UtcNow);
                await new UnitOfWork(promotion).SaveChangesAsync(CancellationToken.None);
            }

            await withdrawal.Games.SingleAsync(game => game.Id == gameId);
            stale.WithdrawFromWaitlist();
            Func<Task> save = () => new UnitOfWork(withdrawal).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task StalePromotion_ShouldConflictWithGameCancellation()
        {
            var options = CreateOptions();
            var (gameId, participantId) = await SeedAsync(options);
            await using var promotion = new ApplicationDbContext(options, new DateTimeProvider());
            var staleGame = await promotion.Games.SingleAsync(game => game.Id == gameId);
            var participant = await promotion.GameParticipants.SingleAsync(participant => participant.Id == participantId);

            await using (var cancellation = new ApplicationDbContext(options, new DateTimeProvider()))
            {
                (await cancellation.Games.SingleAsync(game => game.Id == gameId)).Cancel();
                await new UnitOfWork(cancellation).SaveChangesAsync(CancellationToken.None);
            }

            participant.PromoteFromWaitlist(staleGame, 1, DateTimeOffset.UtcNow);
            Func<Task> save = () => new UnitOfWork(promotion).SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public void PostgreSqlModel_ShouldMatchMigrationAndProtectBothGameAndParticipant()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres").Options;
            using var context = new ApplicationDbContext(options, new DateTimeProvider());

            context.Model.FindEntityType(typeof(Game))!.FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
            context.Model.FindEntityType(typeof(GameParticipant))!.FindProperty(nameof(GameParticipant.JoinStatus))!.IsConcurrencyToken.Should().BeTrue();
            context.Database.HasPendingModelChanges().Should().BeFalse();
            context.Database.GenerateCreateScript().Should().Contain("version uuid NOT NULL");
        }

        private static DbContextOptions<ApplicationDbContext> CreateOptions() => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"waitlist-concurrency-{Guid.NewGuid()}").Options;

        private static async Task<(Guid GameId, Guid ParticipantId)> SeedAsync(DbContextOptions<ApplicationDbContext> options, int maxPlayers = 2)
        {
            await using var context = new ApplicationDbContext(options, new DateTimeProvider());
            var now = DateTimeOffset.UtcNow;
            var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), now.AddDays(2), null, maxPlayers, 0, GameLevel.Intermediate, GameJoinPolicy.Open, null);
            game.MarkAsFull();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), now, maxPlayers);
            game.Reopen();
            context.Games.Add(game);
            context.GameParticipants.Add(participant);
            context.GameParticipants.Add(GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(), now, GameParticipantOfflinePaymentStatus.NotRequired));
            await context.SaveChangesAsync();
            return (game.Id, participant.Id);
        }
    }
}
