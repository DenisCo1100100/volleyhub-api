using FluentAssertions;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameAttendanceTests
    {
        private static readonly DateTimeOffset StartsAt = new(2026, 9, 20, 20, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Attendance_ShouldAllowCorrectionsAndRepeatedMarksWithoutChangingParticipationFacts()
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            game.Complete();
            var approvedAt = participant.ApprovedAt;
            participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);

            foreach (var status in new[] { GameParticipantAttendanceStatus.Absent, GameParticipantAttendanceStatus.Present,
                GameParticipantAttendanceStatus.Present, GameParticipantAttendanceStatus.Absent })
            {
                participant.MarkAttendance(game, status);
                participant.AttendanceStatus.Should().Be(status);
                participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
                participant.ApprovedAt.Should().Be(approvedAt);
                participant.CancelledAt.Should().BeNull();
                participant.CancellationType.Should().BeNull();
                participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            }
        }

        [Theory]
        [InlineData(GameStatus.Open)]
        [InlineData(GameStatus.Full)]
        [InlineData(GameStatus.Cancelled)]
        public void Attendance_ShouldRequireCompletedGame(GameStatus status)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            if (status == GameStatus.Full) game.MarkAsFull();
            if (status == GameStatus.Cancelled) game.Cancel();

            var act = () => participant.MarkAttendance(game, GameParticipantAttendanceStatus.Absent);

            act.Should().Throw<BusinessRuleException>();
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
        }

        [Fact]
        public void Attendance_ShouldRejectDifferentGame()
        {
            var participant = CreateParticipant(CreateGame());
            var anotherGame = CreateGame();
            anotherGame.Complete();

            var act = () => participant.MarkAttendance(anotherGame, GameParticipantAttendanceStatus.Present);

            act.Should().Throw<BusinessRuleException>();
        }

        [Theory]
        [InlineData(GameParticipantAttendanceStatus.Present)]
        [InlineData(GameParticipantAttendanceStatus.Absent)]
        public void MarkedAttendance_ShouldPreventCancellationAndRemoval(GameParticipantAttendanceStatus status)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            game.Complete();
            participant.MarkAttendance(game, status);

            var cancel = () => participant.CancelParticipation(StartsAt.AddHours(-1), StartsAt);
            var remove = () => participant.Remove(StartsAt.AddHours(-1), StartsAt);

            cancel.Should().Throw<BusinessRuleException>();
            remove.Should().Throw<BusinessRuleException>();
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.AttendanceStatus.Should().Be(status);
            participant.CancelledAt.Should().BeNull();
            participant.RemovedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(-25, GameParticipantCancellationType.OnTime)]
        [InlineData(-24, GameParticipantCancellationType.Late)]
        [InlineData(-1, GameParticipantCancellationType.Late)]
        public void CancelledParticipant_ShouldNeverBecomeNoShowOrPresent(int hours, GameParticipantCancellationType type)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            participant.CancelParticipation(StartsAt.AddHours(hours), StartsAt);
            game.Complete();

            foreach (var status in new[] { GameParticipantAttendanceStatus.Present, GameParticipantAttendanceStatus.Absent })
            {
                var act = () => participant.MarkAttendance(game, status);
                act.Should().Throw<BusinessRuleException>();
            }
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.CancellationType.Should().Be(type);
        }

        [Fact]
        public void Correction_ShouldNotEraseRecordedAttendance()
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            game.Complete();
            participant.MarkAttendance(game, GameParticipantAttendanceStatus.Present);

            var act = () => participant.MarkAttendance(game, GameParticipantAttendanceStatus.NotMarked);

            act.Should().Throw<ArgumentException>();
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
        }

        private static Game CreateGame() => Game.Create(Guid.NewGuid(), Guid.NewGuid(), StartsAt, null, 12, 15,
            GameLevel.Any, GameJoinPolicy.Open, null);

        private static GameParticipant CreateParticipant(Game game) => GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(),
            StartsAt.AddDays(-3), GameParticipantOfflinePaymentStatus.Pending);
    }
}
