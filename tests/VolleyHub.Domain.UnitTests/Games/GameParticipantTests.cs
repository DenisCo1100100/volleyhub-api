using FluentAssertions;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameParticipantTests
    {
        private static readonly DateTimeOffset GameStartsAt =
            new(2026, 9, 20, 20, 0, 0, TimeSpan.Zero);

        [Fact]
        public void RequestToJoin_ShouldCreatePendingParticipant_WhenDataIsValid()
        {
            var gameId = Guid.NewGuid();
            var playerProfileId = Guid.NewGuid();
            var joinedAt = GameStartsAt.AddDays(-3);

            var participant = GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            participant.Id.Should().NotBeEmpty();
            participant.GameId.Should().Be(gameId);
            participant.PlayerProfileId.Should().Be(playerProfileId);
            participant.JoinedAt.Should().Be(joinedAt);
            participant.ApprovedAt.Should().BeNull();
            participant.CancelledAt.Should().BeNull();
            participant.RemovedAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
        }

        [Fact]
        public void JoinOpenGame_ShouldCreateApprovedParticipant_WhenDataIsValid()
        {
            var gameId = Guid.NewGuid();
            var playerProfileId = Guid.NewGuid();
            var joinedAt = GameStartsAt.AddDays(-3);

            var participant = GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            participant.Id.Should().NotBeEmpty();
            participant.GameId.Should().Be(gameId);
            participant.PlayerProfileId.Should().Be(playerProfileId);
            participant.JoinedAt.Should().Be(joinedAt);
            participant.ApprovedAt.Should().Be(joinedAt);
            participant.CancelledAt.Should().BeNull();
            participant.RemovedAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
        }

        [Fact]
        public void Approve_ShouldApproveParticipant_WhenJoinRequestIsPending()
        {
            var participant = CreatePendingParticipant();
            var approvedAt = participant.JoinedAt.AddMinutes(10);

            participant.Approve(approvedAt);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.ApprovedAt.Should().Be(approvedAt);
        }

        [Fact]
        public void Reject_ShouldRejectParticipant_WhenJoinRequestIsPending()
        {
            var participant = CreatePendingParticipant();

            participant.Reject();

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Rejected);
            participant.ApprovedAt.Should().BeNull();
        }

        [Fact]
        public void WithdrawRequest_ShouldCancelPendingRequestWithoutCancellationMetadata()
        {
            var participant = CreatePendingParticipant();

            participant.WithdrawRequest();

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
            participant.RemovedAt.Should().BeNull();
        }

        [Fact]
        public void CancelParticipation_ShouldCreateOnTimeCancellation_WhenMoreThanTwentyFourHoursRemain()
        {
            var participant = CreateApprovedParticipant();
            var cancelledAt = GameStartsAt.AddHours(-25);

            participant.CancelParticipation(cancelledAt, GameStartsAt);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().Be(cancelledAt);
            participant.CancellationType.Should().Be(GameParticipantCancellationType.OnTime);
            participant.RemovedAt.Should().BeNull();
        }

        [Fact]
        public void CancelParticipation_ShouldCreateLateCancellation_WhenExactlyTwentyFourHoursRemain()
        {
            var participant = CreateApprovedParticipant();
            var cancelledAt = GameStartsAt.AddHours(-24);

            participant.CancelParticipation(cancelledAt, GameStartsAt);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().Be(cancelledAt);
            participant.CancellationType.Should().Be(GameParticipantCancellationType.Late);
        }

        [Fact]
        public void CancelParticipation_ShouldCreateLateCancellation_WhenLessThanTwentyFourHoursRemain()
        {
            var participant = CreateApprovedParticipant();
            var cancelledAt = GameStartsAt.AddHours(-2);

            participant.CancelParticipation(cancelledAt, GameStartsAt);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().Be(cancelledAt);
            participant.CancellationType.Should().Be(GameParticipantCancellationType.Late);
        }

        [Fact]
        public void CancelParticipation_ShouldThrowBusinessRuleException_WhenGameHasStarted()
        {
            var participant = CreateApprovedParticipant();

            Action act = () => participant.CancelParticipation(
                GameStartsAt,
                GameStartsAt);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void CancelParticipation_ShouldThrowBusinessRuleException_WhenParticipantIsNotApproved()
        {
            var participant = CreatePendingParticipant();

            Action act = () => participant.CancelParticipation(
                GameStartsAt.AddHours(-2),
                GameStartsAt);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void CancelParticipation_ShouldThrowBusinessRuleException_WhenParticipantIsAlreadyCancelled()
        {
            var participant = CreateApprovedParticipant();
            var cancelledAt = GameStartsAt.AddHours(-2);

            participant.CancelParticipation(cancelledAt, GameStartsAt);

            Action act = () => participant.CancelParticipation(
                cancelledAt.AddMinutes(1),
                GameStartsAt);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Remove_ShouldMarkApprovedParticipantAsRemoved()
        {
            var participant = CreateApprovedParticipant();
            var removedAt = GameStartsAt.AddHours(-2);

            participant.Remove(removedAt, GameStartsAt);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Removed);
            participant.RemovedAt.Should().Be(removedAt);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
        }

        [Fact]
        public void Remove_ShouldThrowBusinessRuleException_WhenGameHasStarted()
        {
            var participant = CreateApprovedParticipant();

            Action act = () => participant.Remove(
                GameStartsAt,
                GameStartsAt);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Remove_ShouldThrowBusinessRuleException_WhenParticipantIsNotApproved()
        {
            var participant = CreatePendingParticipant();

            Action act = () => participant.Remove(
                GameStartsAt.AddHours(-2),
                GameStartsAt);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void MarkAttendance_ShouldSetAttendanceStatus_WhenParticipantIsApproved()
        {
            var participant = CreateApprovedParticipant();

            participant.MarkAttendance(GameParticipantAttendanceStatus.Present);

            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
        }

        [Fact]
        public void MarkAttendance_ShouldThrowBusinessRuleException_WhenParticipantIsCancelled()
        {
            var participant = CreateApprovedParticipant();

            participant.CancelParticipation(
                GameStartsAt.AddHours(-2),
                GameStartsAt);

            Action act = () => participant.MarkAttendance(
                GameParticipantAttendanceStatus.Absent);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void MarkAttendance_ShouldThrowBusinessRuleException_WhenParticipantIsRemoved()
        {
            var participant = CreateApprovedParticipant();

            participant.Remove(
                GameStartsAt.AddHours(-2),
                GameStartsAt);

            Action act = () => participant.MarkAttendance(
                GameParticipantAttendanceStatus.Absent);

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void UpdateOfflinePaymentStatus_ShouldSetPaymentStatusToPaid_WhenPaymentIsPending()
        {
            var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), GameStartsAt, null, 12, 15, GameLevel.Any, GameJoinPolicy.Open, null);
            var participant = GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(), GameStartsAt.AddDays(-3), GameParticipantOfflinePaymentStatus.Pending);

            participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);

            participant.OfflinePaymentStatus.Should().Be(
                GameParticipantOfflinePaymentStatus.Paid);
        }

        [Fact]
        public void RequestToJoin_ShouldThrowArgumentException_WhenGameIdIsEmpty()
        {
            Action act = () => GameParticipant.RequestToJoin(
                gameId: Guid.Empty,
                playerProfileId: Guid.NewGuid(),
                joinedAt: GameStartsAt.AddDays(-3),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RequestToJoin_ShouldThrowArgumentException_WhenPlayerProfileIdIsEmpty()
        {
            Action act = () => GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.Empty,
                joinedAt: GameStartsAt.AddDays(-3),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RequestToJoin_ShouldThrowArgumentException_WhenJoinedAtIsDefault()
        {
            Action act = () => GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: default,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(GameParticipantOfflinePaymentStatus.Unknown)]
        [InlineData((GameParticipantOfflinePaymentStatus)999)]
        public void RequestToJoin_ShouldThrowArgumentException_WhenOfflinePaymentStatusIsInvalid(
            GameParticipantOfflinePaymentStatus offlinePaymentStatus)
        {
            Action act = () => GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: GameStartsAt.AddDays(-3),
                offlinePaymentStatus: offlinePaymentStatus);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Approve_ShouldThrowBusinessRuleException_WhenParticipantIsAlreadyApproved()
        {
            var participant = CreateApprovedParticipant();

            Action act = () => participant.Approve(
                participant.ApprovedAt!.Value.AddMinutes(1));

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void Approve_ShouldThrowArgumentException_WhenApprovedAtIsBeforeJoinedAt()
        {
            var participant = CreatePendingParticipant();
            var approvedAt = participant.JoinedAt.AddMinutes(-1);

            Action act = () => participant.Approve(approvedAt);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Reject_ShouldThrowBusinessRuleException_WhenParticipantIsAlreadyApproved()
        {
            var participant = CreateApprovedParticipant();

            Action act = participant.Reject;

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void WithdrawRequest_ShouldThrowBusinessRuleException_WhenParticipantIsApproved()
        {
            var participant = CreateApprovedParticipant();

            Action act = participant.WithdrawRequest;

            act.Should().Throw<BusinessRuleException>();
        }

        [Fact]
        public void MarkAttendance_ShouldThrowBusinessRuleException_WhenParticipantIsPending()
        {
            var participant = CreatePendingParticipant();

            Action act = () => participant.MarkAttendance(
                GameParticipantAttendanceStatus.Present);

            act.Should().Throw<BusinessRuleException>();
        }

        [Theory]
        [InlineData(GameParticipantAttendanceStatus.Unknown)]
        [InlineData(GameParticipantAttendanceStatus.NotMarked)]
        [InlineData((GameParticipantAttendanceStatus)999)]
        public void MarkAttendance_ShouldThrowArgumentException_WhenAttendanceStatusIsInvalid(
            GameParticipantAttendanceStatus attendanceStatus)
        {
            var participant = CreateApprovedParticipant();

            Action act = () => participant.MarkAttendance(attendanceStatus);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void UpdateOfflinePaymentStatus_ShouldThrowBusinessRuleException_WhenGameIsFree()
        {
            var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), GameStartsAt, null, 12, 0, GameLevel.Any, GameJoinPolicy.Open, null);
            var participant = GameParticipant.JoinOpenGame(game.Id, Guid.NewGuid(), GameStartsAt.AddDays(-3), GameParticipantOfflinePaymentStatus.NotRequired);

            Action act = () => participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);

            act.Should().Throw<BusinessRuleException>();
        }

        private static GameParticipant CreatePendingParticipant()
        {
            return GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: GameStartsAt.AddDays(-3),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateApprovedParticipant(
            GameParticipantOfflinePaymentStatus offlinePaymentStatus =
                GameParticipantOfflinePaymentStatus.Pending)
        {
            return GameParticipant.JoinOpenGame(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: GameStartsAt.AddDays(-3),
                offlinePaymentStatus: offlinePaymentStatus);
        }
    }
}
