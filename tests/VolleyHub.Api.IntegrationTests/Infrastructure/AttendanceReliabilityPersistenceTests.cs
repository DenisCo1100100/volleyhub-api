using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Domain.Games;
using VolleyHub.Infrastructure.Persistence;
using VolleyHub.Infrastructure.Persistence.Repositories;
using VolleyHub.Infrastructure.Services;

namespace VolleyHub.Api.IntegrationTests.Infrastructure
{
    public sealed class AttendanceReliabilityPersistenceTests
    {
        private static readonly DateTimeOffset StartsAt = new(2026, 9, 20, 20, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Reliability_ShouldAggregateInPostgreSqlWithoutLoadingParticipationHistory()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=volleyhub_sql_translation;Username=postgres;Password=postgres").Options, new DateTimeProvider());

            var sql = new GameParticipantRepository(context).BuildReliabilitySummaryQuery(Guid.NewGuid()).ToQueryString();

            sql.Should().Contain("JOIN games").And.Contain("GROUP BY").And.Contain("count(*)")
                .And.Contain("player_profile_id =").And.Contain("Completed").And.Contain("Approved").And.Contain("Cancelled")
                .And.Contain("Present").And.Contain("Absent").And.Contain("NotMarked").And.Contain("Late").And.Contain("OnTime")
                .And.Contain("approved_at IS NOT NULL").And.Contain("cancelled_at IS NOT NULL")
                .And.NotContain("starts_at").And.NotContain("joined_at").And.NotContain("offline_payment_status");
            context.Model.FindEntityType(typeof(GameParticipant))!.FindProperty(nameof(GameParticipant.AttendanceStatus))!
                .IsConcurrencyToken.Should().BeTrue();
            context.Database.HasPendingModelChanges().Should().BeFalse();
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        [Fact]
        public async Task Reliability_ShouldUseDisjointRecordedFactsAndPreserveUnknownLegacyCancellations()
        {
            await using var context = CreateContext();
            var profileId = Guid.NewGuid();
            AddParticipant(context, profileId, attendance: GameParticipantAttendanceStatus.Present);
            AddParticipant(context, profileId, attendance: GameParticipantAttendanceStatus.Absent);
            AddParticipant(context, profileId);
            AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, cancellation: GameParticipantCancellationType.OnTime);
            // Even inconsistent old attendance must not turn a cancellation into a second no-show.
            AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, GameParticipantAttendanceStatus.Absent, GameParticipantCancellationType.Late);
            AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, GameParticipantAttendanceStatus.Present, GameParticipantCancellationType.OnTime);
            AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, approved: false);
            AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, cancellation: GameParticipantCancellationType.Late, approved: false);
            AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, cancellation: GameParticipantCancellationType.Late, cancelledAt: false);
            var legacy = AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, cancelledAt: false);
            foreach (var status in new[] { GameParticipantJoinStatus.PendingApproval, GameParticipantJoinStatus.Rejected,
                GameParticipantJoinStatus.Removed, GameParticipantJoinStatus.Waitlisted })
            {
                AddParticipant(context, profileId, status, GameParticipantAttendanceStatus.Absent);
            }
            AddParticipant(context, Guid.NewGuid(), attendance: GameParticipantAttendanceStatus.Present);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var summary = await new GameParticipantRepository(context).GetReliabilitySummaryAsync(profileId, CancellationToken.None);

            summary.AttendedGamesCount.Should().Be(1);
            summary.NoShowCount.Should().Be(1);
            summary.OnTimeCancellationCount.Should().Be(2);
            summary.LateCancellationCount.Should().Be(1);
            summary.UnmarkedGamesCount.Should().Be(1);
            summary.TotalMarkedGamesCount.Should().Be(2);
            summary.AttendanceRate.Should().Be(50);
            context.ChangeTracker.Entries().Should().BeEmpty();
            var savedLegacy = await context.GameParticipants.FindAsync(legacy.Id);
            savedLegacy!.CancelledAt.Should().BeNull();
            savedLegacy.CancellationType.Should().BeNull();
        }

        [Theory]
        [InlineData(GameStatus.Draft)]
        [InlineData(GameStatus.Open)]
        [InlineData(GameStatus.Full)]
        [InlineData(GameStatus.Cancelled)]
        public async Task Reliability_ShouldExcludeEveryFactFromUncompletedGamesRegardlessOfSchedule(GameStatus status)
        {
            await using var context = CreateContext();
            var profileId = Guid.NewGuid();
            foreach (var startsAt in new[] { StartsAt.AddYears(-2), StartsAt.AddYears(2) })
            {
                AddParticipant(context, profileId, attendance: GameParticipantAttendanceStatus.Absent, gameStatus: status, startsAt: startsAt);
                AddParticipant(context, profileId, attendance: GameParticipantAttendanceStatus.Present, gameStatus: status, startsAt: startsAt);
                AddParticipant(context, profileId, gameStatus: status, startsAt: startsAt);
                AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, cancellation: GameParticipantCancellationType.Late,
                    gameStatus: status, startsAt: startsAt);
                AddParticipant(context, profileId, GameParticipantJoinStatus.Cancelled, cancellation: GameParticipantCancellationType.OnTime,
                    gameStatus: status, startsAt: startsAt);
            }
            await context.SaveChangesAsync();

            var summary = await new GameParticipantRepository(context).GetReliabilitySummaryAsync(profileId, CancellationToken.None);

            summary.Should().Be(new Application.PlayerProfiles.Dtos.PlayerReliabilitySummaryDto(profileId, 0, 0, 0, 0, 0));
        }

        [Fact]
        public async Task Reliability_ShouldKeepCancellationClassificationWhenGameIsRescheduled()
        {
            await using var context = CreateContext();
            var game = CreateGame();
            var participant = GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(), StartsAt.AddDays(-3), GameParticipantOfflinePaymentStatus.Pending);
            participant.CancelParticipation(StartsAt.AddHours(-24), StartsAt);
            game.UpdateDetails(game.OrganizerId, game.CourtId, StartsAt.AddDays(10), null, game.MaxPlayers, game.PricePerPlayer,
                game.RequiredLevel, game.JoinPolicy, null);
            game.Complete();
            context.AddRange(game, participant);
            await context.SaveChangesAsync();

            var summary = await new GameParticipantRepository(context).GetReliabilitySummaryAsync(participant.PlayerProfileId, CancellationToken.None);

            summary.LateCancellationCount.Should().Be(1);
            summary.OnTimeCancellationCount.Should().Be(0);
            summary.NoShowCount.Should().Be(0);
            summary.TotalMarkedGamesCount.Should().Be(0);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task StaleAttendanceOrPayment_ShouldNotOverwriteCorrectionEvenWhenGameIsReadLater(bool payment)
        {
            var options = Options();
            Guid participantId;
            await using (var seed = new ApplicationDbContext(options, new DateTimeProvider()))
            {
                var participant = AddParticipant(seed, Guid.NewGuid(), attendance: GameParticipantAttendanceStatus.Absent);
                participantId = participant.Id;
                await seed.SaveChangesAsync();
            }
            await using var staleContext = new ApplicationDbContext(options, new DateTimeProvider());
            var stale = (await staleContext.GameParticipants.FindAsync(participantId))!;
            await using (var correction = new ApplicationDbContext(options, new DateTimeProvider()))
            {
                var participant = (await correction.GameParticipants.FindAsync(participantId))!;
                var game = (await correction.Games.FindAsync(participant.GameId))!;
                participant.MarkAttendance(game, GameParticipantAttendanceStatus.Present);
                await new UnitOfWork(correction).SaveChangesAsync(CancellationToken.None);
            }
            var latestGame = (await staleContext.Games.FindAsync(stale.GameId))!;
            if (payment) stale.UpdateOfflinePaymentStatus(latestGame, GameParticipantOfflinePaymentStatus.Paid);
            else stale.MarkAttendance(latestGame, GameParticipantAttendanceStatus.Absent);
            new GameParticipantRepository(staleContext).Update(stale);

            var save = () => new UnitOfWork(staleContext).SaveChangesAsync(CancellationToken.None);

            await save.Should().ThrowAsync<ConflictException>();
            await using var verify = new ApplicationDbContext(options, new DateTimeProvider());
            var saved = (await verify.GameParticipants.FindAsync(participantId))!;
            saved.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
            saved.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant AddParticipant(ApplicationDbContext context, Guid profileId,
            GameParticipantJoinStatus join = GameParticipantJoinStatus.Approved,
            GameParticipantAttendanceStatus attendance = GameParticipantAttendanceStatus.NotMarked,
            GameParticipantCancellationType? cancellation = null, bool approved = true, bool cancelledAt = true,
            GameStatus gameStatus = GameStatus.Completed, DateTimeOffset? startsAt = null)
        {
            var game = CreateGame(startsAt);
            var participant = GameParticipant.JoinOpenGame(game.Id, profileId, game.StartsAt.AddDays(-3), GameParticipantOfflinePaymentStatus.Pending);
            context.AddRange(game, participant);
            // Seed persisted legacy/inconsistent combinations deliberately, bypassing current domain guards.
            context.Entry(game).Property(g => g.Status).CurrentValue = gameStatus;
            var entry = context.Entry(participant);
            entry.Property(p => p.JoinStatus).CurrentValue = join;
            entry.Property(p => p.AttendanceStatus).CurrentValue = attendance;
            entry.Property(p => p.CancellationType).CurrentValue = cancellation;
            if (!approved) entry.Property(p => p.ApprovedAt).CurrentValue = null;
            if (cancelledAt && join == GameParticipantJoinStatus.Cancelled)
                entry.Property(p => p.CancelledAt).CurrentValue = game.StartsAt.AddHours(-1);
            return participant;
        }

        private static Game CreateGame(DateTimeOffset? startsAt = null) => Game.Create(Guid.NewGuid(), Guid.NewGuid(), startsAt ?? StartsAt,
            null, 12, 15, GameLevel.Any, GameJoinPolicy.Open, null);

        private static DbContextOptions<ApplicationDbContext> Options() => new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"attendance-{Guid.NewGuid()}").Options;

        private static ApplicationDbContext CreateContext() => new(Options(), new DateTimeProvider());
    }
}
