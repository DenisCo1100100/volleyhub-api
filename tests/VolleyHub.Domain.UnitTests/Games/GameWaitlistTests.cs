using FluentAssertions;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameWaitlistTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(GameJoinPolicy.Open)]
        [InlineData(GameJoinPolicy.ApprovalRequired)]
        public void JoinWaitlist_ShouldCreateUnconfirmedParticipant(GameJoinPolicy policy)
        {
            var game = CreateFullGame(policy);
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Waitlisted);
            participant.JoinedAt.Should().Be(Now);
            participant.ApprovedAt.Should().BeNull();
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.CancellationType.Should().BeNull();
            game.Status.Should().Be(GameStatus.Full);
        }

        [Theory]
        [InlineData(GameStatus.Open)]
        [InlineData(GameStatus.Cancelled)]
        [InlineData(GameStatus.Completed)]
        public void JoinWaitlist_ShouldRejectGamesThatAreNotFull(GameStatus status)
        {
            var game = CreateFullGame();
            SetStatus(game, status);
            var act = () => GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void JoinWaitlist_ShouldRejectInviteOnlyGamesAndInconsistentCapacity()
        {
            var inviteGame = CreateFullGame(GameJoinPolicy.InviteOnly);
            var invite = () => GameParticipant.JoinWaitlist(inviteGame, Guid.NewGuid(), Now, 2);
            invite.Should().Throw<BusinessRuleException>();

            var game = CreateFullGame();
            var notFull = () => GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 1);
            notFull.Should().Throw<BusinessRuleException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Waitlist_ShouldRejectJoiningAndPromotionAtOrAfterStart(int minutesAfterStart)
        {
            var game = CreateFullGame();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            var eventTime = game.StartsAt.AddMinutes(minutesAfterStart);
            var join = () => GameParticipant.JoinWaitlist(game, Guid.NewGuid(), eventTime, 2);
            join.Should().Throw<BusinessRuleException>();

            game.Reopen();
            var promote = () => participant.PromoteFromWaitlist(game, 1, eventTime);
            promote.Should().Throw<BusinessRuleException>();
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Waitlisted);
        }

        [Theory]
        [InlineData(GameJoinPolicy.Open, 0, GameParticipantOfflinePaymentStatus.NotRequired)]
        [InlineData(GameJoinPolicy.ApprovalRequired, 15, GameParticipantOfflinePaymentStatus.Pending)]
        public void Promote_ShouldApproveAndFillLastPlace(GameJoinPolicy policy, int price, GameParticipantOfflinePaymentStatus payment)
        {
            var game = CreateFullGame(policy, price);
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            game.Reopen();

            participant.PromoteFromWaitlist(game, 1, Now.AddMinutes(1));

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.ApprovedAt.Should().Be(Now.AddMinutes(1));
            participant.JoinedAt.Should().Be(Now);
            participant.OfflinePaymentStatus.Should().Be(payment);
            game.Status.Should().Be(GameStatus.Full);
        }

        [Fact]
        public void Promote_ShouldLeaveGameOpen_WhenMoreCapacityRemains()
        {
            var game = CreateFullGame();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            game.Reopen();
            participant.PromoteFromWaitlist(game, 0, Now);
            game.Status.Should().Be(GameStatus.Open);
        }

        [Theory]
        [InlineData(GameStatus.Full, 2)]
        [InlineData(GameStatus.Open, 2)]
        [InlineData(GameStatus.Cancelled, 1)]
        [InlineData(GameStatus.Completed, 1)]
        public void Promote_ShouldRejectUnavailableGames(GameStatus status, int approvedCount)
        {
            var game = CreateFullGame();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            SetStatus(game, status);

            var act = () => participant.PromoteFromWaitlist(game, approvedCount, Now);
            act.Should().Throw<BusinessRuleException>();
            participant.ApprovedAt.Should().BeNull();
        }

        [Fact]
        public void Promote_ShouldRejectWrongGameAndInvalidTimestamp()
        {
            var game = CreateFullGame();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            game.Reopen();
            var wrongGame = CreateFullGame();
            wrongGame.Reopen();

            var wrong = () => participant.PromoteFromWaitlist(wrongGame, 1, Now);
            wrong.Should().Throw<BusinessRuleException>();
            var early = () => participant.PromoteFromWaitlist(game, 1, Now.AddTicks(-1));
            early.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExitWaitlist_ShouldPreserveHistoryWithoutCancellationPenalty(bool rejected)
        {
            var game = CreateFullGame();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);

            if (rejected)
            {
                participant.RejectFromWaitlist();
            }
            else
            {
                participant.WithdrawFromWaitlist();
            }

            participant.JoinStatus.Should().Be(rejected ? GameParticipantJoinStatus.Rejected : GameParticipantJoinStatus.Cancelled);
            participant.ApprovedAt.Should().BeNull();
            participant.CancelledAt.Should().BeNull();
            participant.RemovedAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
            game.Status.Should().Be(GameStatus.Full);

            game.Reopen();
            var promote = () => participant.PromoteFromWaitlist(game, 1, Now);
            promote.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void WaitlistedParticipant_ShouldNotUseConfirmedParticipantTransitions()
        {
            var game = CreateFullGame();
            var participant = GameParticipant.JoinWaitlist(game, Guid.NewGuid(), Now, 2);
            var approve = () => participant.Approve(Now);
            var attendance = () => participant.MarkAttendance(game, GameParticipantAttendanceStatus.Present);
            var cancel = () => participant.CancelParticipation(Now, game.StartsAt);

            approve.Should().Throw<BusinessRuleException>();
            attendance.Should().Throw<BusinessRuleException>();
            cancel.Should().Throw<BusinessRuleException>();
        }

        private static Game CreateFullGame(GameJoinPolicy policy = GameJoinPolicy.Open, decimal price = 15)
        {
            var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(2), null, 2, price, GameLevel.Intermediate, policy, null);
            game.MarkAsFull();
            return game;
        }

        private static void SetStatus(Game game, GameStatus status)
        {
            switch (status)
            {
                case GameStatus.Open: game.Reopen(); break;
                case GameStatus.Cancelled: game.Cancel(); break;
                case GameStatus.Completed: game.Complete(); break;
            }
        }
    }
}
