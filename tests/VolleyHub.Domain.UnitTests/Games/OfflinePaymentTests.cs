using FluentAssertions;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class OfflinePaymentTests
    {
        [Theory]
        [InlineData(GameStatus.Open)]
        [InlineData(GameStatus.Full)]
        [InlineData(GameStatus.Completed)]
        public void Update_ShouldAllowPaymentCorrectionsAndRepeatedStatuses(GameStatus status)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            if (status is GameStatus.Full) game.MarkAsFull();
            if (status is GameStatus.Completed) game.Complete();

            foreach (var paymentStatus in new[] { GameParticipantOfflinePaymentStatus.Paid, GameParticipantOfflinePaymentStatus.Paid,
                GameParticipantOfflinePaymentStatus.Pending, GameParticipantOfflinePaymentStatus.Pending })
            {
                participant.UpdateOfflinePaymentStatus(game, paymentStatus);
                participant.OfflinePaymentStatus.Should().Be(paymentStatus);
                participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
                participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            }
        }

        [Theory]
        [InlineData(GameParticipantJoinStatus.PendingApproval)]
        [InlineData(GameParticipantJoinStatus.Rejected)]
        [InlineData(GameParticipantJoinStatus.Cancelled)]
        [InlineData(GameParticipantJoinStatus.Removed)]
        [InlineData(GameParticipantJoinStatus.Waitlisted)]
        public void Update_ShouldRejectParticipantsWithoutConfirmedPlaces(GameParticipantJoinStatus status)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game, status);
            var previousStatus = participant.OfflinePaymentStatus;

            Action act = () => participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);

            act.Should().Throw<BusinessRuleException>();
            participant.OfflinePaymentStatus.Should().Be(previousStatus);
        }

        [Fact]
        public void Update_ShouldRejectCancelledAndUnrelatedGames()
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            Action unrelated = () => participant.UpdateOfflinePaymentStatus(CreateGame(), GameParticipantOfflinePaymentStatus.Paid);
            unrelated.Should().Throw<BusinessRuleException>();

            game.Cancel();
            Action cancelled = () => participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);
            cancelled.Should().Throw<BusinessRuleException>();
        }

        [Theory]
        [InlineData(0, GameParticipantOfflinePaymentStatus.Pending)]
        [InlineData(0, GameParticipantOfflinePaymentStatus.Paid)]
        [InlineData(15, GameParticipantOfflinePaymentStatus.NotRequired)]
        public void Update_ShouldRejectStatusesIncompatibleWithPrice(int price, GameParticipantOfflinePaymentStatus status)
        {
            var game = CreateGame(price);
            var participant = CreateParticipant(game);
            Action act = () => participant.UpdateOfflinePaymentStatus(game, status);
            act.Should().Throw<BusinessRuleException>();
        }

        [Theory]
        [InlineData(GameParticipantOfflinePaymentStatus.Unknown)]
        [InlineData((GameParticipantOfflinePaymentStatus)999)]
        public void Update_ShouldRejectInvalidStatuses(GameParticipantOfflinePaymentStatus status)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            Action act = () => participant.UpdateOfflinePaymentStatus(game, status);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FreeGame_ShouldKeepNotRequired()
        {
            var game = CreateGame(0);
            var participant = CreateParticipant(game);
            participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.NotRequired);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
        }

        [Theory]
        [InlineData(GameParticipantJoinStatus.Approved)]
        [InlineData(GameParticipantJoinStatus.PendingApproval)]
        [InlineData(GameParticipantJoinStatus.Waitlisted)]
        public void PriceChange_ShouldSynchronizeActiveParticipantsAndPreserveWaitlist(GameParticipantJoinStatus status)
        {
            var game = CreateGame(0);
            var participant = CreateParticipant(game, status);
            if (game.Status is GameStatus.Full) game.Reopen();
            game.UpdateDetails(game.OrganizerId, game.CourtId, game.StartsAt, game.EndsAt, game.MaxPlayers,
                20, game.RequiredLevel, game.JoinPolicy, game.Description);

            participant.SynchronizeOfflinePaymentRequirement(game);

            participant.OfflinePaymentStatus.Should().Be(status is GameParticipantJoinStatus.Waitlisted
                ? GameParticipantOfflinePaymentStatus.NotRequired : GameParticipantOfflinePaymentStatus.Pending);
            participant.JoinStatus.Should().Be(status);

            game.UpdateDetails(game.OrganizerId, game.CourtId, game.StartsAt, game.EndsAt, game.MaxPlayers,
                0, game.RequiredLevel, game.JoinPolicy, game.Description);
            participant.SynchronizeOfflinePaymentRequirement(game);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PriceChange_ShouldRejectRepricingRecordedPaymentsIncludingCancelledParticipants(bool cancel)
        {
            var game = CreateGame();
            var participant = CreateParticipant(game);
            participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);
            if (cancel) participant.CancelParticipation(DateTimeOffset.UtcNow, game.StartsAt);

            Action change = () => game.EnsureCanChangePrice(20, [participant]);
            change.Should().Throw<BusinessRuleException>();
            game.EnsureCanChangePrice(game.PricePerPlayer, [participant]);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
        }

        private static Game CreateGame(decimal price = 15) => Game.Create(Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(2), null, 12, price, GameLevel.Any, GameJoinPolicy.Open, null);

        private static GameParticipant CreateParticipant(Game game, GameParticipantJoinStatus status = GameParticipantJoinStatus.Approved)
        {
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            var payment = game.PricePerPlayer > 0 ? GameParticipantOfflinePaymentStatus.Pending : GameParticipantOfflinePaymentStatus.NotRequired;
            if (status is GameParticipantJoinStatus.Waitlisted)
            {
                game.MarkAsFull();
                return GameParticipant.JoinWaitlist(game, Guid.NewGuid(), now, game.MaxPlayers);
            }

            if (status is GameParticipantJoinStatus.PendingApproval or GameParticipantJoinStatus.Rejected)
            {
                var pending = GameParticipant.RequestToJoin(game.Id, Guid.NewGuid(), now, payment);
                if (status is GameParticipantJoinStatus.Rejected) pending.Reject();
                return pending;
            }

            var participant = GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(), now, payment);
            if (status is GameParticipantJoinStatus.Cancelled) participant.CancelParticipation(DateTimeOffset.UtcNow, game.StartsAt);
            if (status is GameParticipantJoinStatus.Removed) participant.Remove(DateTimeOffset.UtcNow, game.StartsAt);
            return participant;
        }
    }
}
