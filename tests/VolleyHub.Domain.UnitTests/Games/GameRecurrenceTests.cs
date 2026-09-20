using FluentAssertions;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameRecurrenceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateOccurrences_ShouldCopySettingsAndPreserveWeeklyUtcSchedule(bool withEnd)
        {
            var source = Source(withEnd);
            source.Complete();
            var first = Now.AddDays(7).ToOffset(TimeSpan.FromHours(3));
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), source, first, 52, Now);
            var games = Enumerable.Range(1, 52).Select(recurrence.CreateOccurrence).ToArray();

            games.Select(game => game.Id).Should().OnlyHaveUniqueItems();
            for (var index = 0; index < games.Length; index++)
            {
                var game = games[index];
                game.RecurrenceId.Should().Be(recurrence.Id);
                game.OccurrenceNumber.Should().Be(index + 1);
                game.StartsAt.Should().Be(first.AddDays(7 * index));
                game.StartsAt.Offset.Should().Be(TimeSpan.Zero);
                game.EndsAt.Should().Be(withEnd ? game.StartsAt.AddHours(2) : null);
                game.Should().BeEquivalentTo(source, options => options.Including(g => g.OrganizerId).Including(g => g.CourtId)
                    .Including(g => g.MaxPlayers).Including(g => g.PricePerPlayer).Including(g => g.RequiredLevel)
                    .Including(g => g.JoinPolicy).Including(g => g.Description));
                game.Status.Should().Be(GameStatus.Open);
            }
            source.RecurrenceId.Should().BeNull();
            source.Status.Should().Be(GameStatus.Completed);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(53)]
        public void Create_ShouldRejectUnboundedOrTooShortSchedule(int count)
        {
            var act = () => GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(7), count, Now);
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Create_ShouldRequireFutureStart(int days)
        {
            var act = () => GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(days), 2, Now);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldRejectMissingIdentityAndScheduleOverflow()
        {
            var missingId = () => GameRecurrence.Create(Guid.Empty, Source(), Now.AddDays(7), 2, Now);
            var overflow = () => GameRecurrence.Create(Guid.NewGuid(), Source(), DateTimeOffset.MaxValue.AddDays(-1), 2, Now);
            missingId.Should().Throw<ArgumentException>();
            overflow.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void Generate_ShouldRejectOccurrenceOutsideSchedule(int number)
        {
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(7), 3, Now);
            var act = () => recurrence.CreateOccurrence(number);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IndividualLifecycleAndRescheduling_ShouldKeepStableOccurrenceIdentity()
        {
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(7), 3, Now);
            var first = recurrence.CreateOccurrence(1);
            var second = recurrence.CreateOccurrence(2);
            first.UpdateDetails(first.OrganizerId, first.CourtId, Now.AddDays(9), null, 10, 0, GameLevel.Any, GameJoinPolicy.Open, "Exception");
            first.Cancel();
            second.Complete();

            first.OccurrenceNumber.Should().Be(1);
            first.RecurrenceId.Should().Be(recurrence.Id);
            recurrence.GetStartsAt(1).Should().Be(Now.AddDays(7));
            recurrence.CancelledAt.Should().BeNull();
            recurrence.Description.Should().Be("Weekly game");
            second.Status.Should().Be(GameStatus.Completed);
        }

        [Fact]
        public void FutureSelection_ShouldRespectRangeActualStartAndTerminalStates()
        {
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(1), 6, Now);
            var games = Enumerable.Range(1, 6).Select(recurrence.CreateOccurrence).ToArray();
            games[2].Cancel();
            games[3].Complete();
            games[4].MarkAsFull();
            games[5].UpdateDetails(games[5].OrganizerId, games[5].CourtId, Now, null, 12, 15, GameLevel.Any, GameJoinPolicy.Open, null);

            recurrence.SelectFutureOccurrences(games.Append(Source()), 2, Now).Should().Equal(games[1], games[4]);
            recurrence.SelectFutureOccurrences(games, 1, games[1].StartsAt).Should().Equal(games[4]);
        }

        [Fact]
        public void Cancel_ShouldCloseDefinitionWithoutChangingIndependentGames()
        {
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(1), 2, Now);
            var game = recurrence.CreateOccurrence(1);
            recurrence.Cancel(Now);

            recurrence.CancelledAt.Should().Be(Now);
            var generate = () => recurrence.CreateOccurrence(2);
            var update = () => recurrence.UpdateSettingsFrom(game);
            generate.Should().Throw<BusinessRuleException>();
            update.Should().Throw<BusinessRuleException>();
            game.Status.Should().Be(GameStatus.Open);
        }

        [Fact]
        public void UpdateSettings_ShouldOnlyAcceptOwnOccurrenceAndKeepSchedule()
        {
            var recurrence = GameRecurrence.Create(Guid.NewGuid(), Source(), Now.AddDays(1), 2, Now);
            var game = recurrence.CreateOccurrence(2);
            game.UpdateDetails(game.OrganizerId, Guid.NewGuid(), Now.AddDays(9), null, 8, 25, GameLevel.Any, GameJoinPolicy.InviteOnly, "Updated");
            recurrence.UpdateSettingsFrom(game);
            recurrence.CourtId.Should().Be(game.CourtId);
            recurrence.PricePerPlayer.Should().Be(25);
            recurrence.FirstStartsAt.Should().Be(Now.AddDays(1));
            recurrence.Duration.Should().Be(TimeSpan.FromHours(2));
            var act = () => recurrence.UpdateSettingsFrom(Source());
            act.Should().Throw<BusinessRuleException>();
        }

        private static Game Source(bool withEnd = true) => Game.Create(Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-7),
            withEnd ? Now.AddDays(-7).AddHours(2) : null, 12, 15, GameLevel.Intermediate, GameJoinPolicy.ApprovalRequired, "Weekly game");
    }
}
