using FluentAssertions;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameParticipantTests
    {
        [Fact]
        public void RequestToJoin_ShouldCreatePendingParticipant_WhenDataIsValid()
        {
            var gameId = Guid.NewGuid();
            var playerProfileId = Guid.NewGuid();
            var joinedAt = DateTimeOffset.UtcNow;

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
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
        }

        [Fact]
        public void JoinOpenGame_ShouldCreateApprovedParticipant_WhenDataIsValid()
        {
            var gameId = Guid.NewGuid();
            var playerProfileId = Guid.NewGuid();
            var joinedAt = DateTimeOffset.UtcNow;

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
        public void Cancel_ShouldCancelParticipant_WhenJoinRequestIsPending()
        {
            var participant = CreatePendingParticipant();

            participant.Cancel();

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
        }

        [Fact]
        public void Cancel_ShouldCancelParticipant_WhenParticipantIsApproved()
        {
            var participant = CreateApprovedParticipant();

            participant.Cancel();

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
        }

        [Fact]
        public void MarkAttendance_ShouldSetAttendanceStatus_WhenParticipantIsApproved()
        {
            var participant = CreateApprovedParticipant();

            participant.MarkAttendance(GameParticipantAttendanceStatus.Present);

            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
        }

        [Fact]
        public void MarkOfflinePaymentAsPaid_ShouldSetPaymentStatusToPaid_WhenPaymentIsPending()
        {
            var participant = CreateApprovedParticipant(
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            participant.MarkOfflinePaymentAsPaid();

            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
        }

        [Fact]
        public void RequestToJoin_ShouldThrowArgumentException_WhenGameIdIsEmpty()
        {
            Action act = () => GameParticipant.RequestToJoin(
                gameId: Guid.Empty,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RequestToJoin_ShouldThrowArgumentException_WhenPlayerProfileIdIsEmpty()
        {
            Action act = () => GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.Empty,
                joinedAt: DateTimeOffset.UtcNow,
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
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: offlinePaymentStatus);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Approve_ShouldThrowInvalidOperationException_WhenParticipantIsAlreadyApproved()
        {
            var participant = CreateApprovedParticipant();

            Action act = () => participant.Approve(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
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
        public void Reject_ShouldThrowInvalidOperationException_WhenParticipantIsAlreadyApproved()
        {
            var participant = CreateApprovedParticipant();

            Action act = () => participant.Reject();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Cancel_ShouldThrowInvalidOperationException_WhenParticipantIsRejected()
        {
            var participant = CreatePendingParticipant();
            participant.Reject();

            Action act = () => participant.Cancel();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MarkAttendance_ShouldThrowInvalidOperationException_WhenParticipantIsPending()
        {
            var participant = CreatePendingParticipant();

            Action act = () => participant.MarkAttendance(GameParticipantAttendanceStatus.Present);

            act.Should().Throw<InvalidOperationException>();
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
        public void MarkOfflinePaymentAsPaid_ShouldThrowInvalidOperationException_WhenPaymentIsNotRequired()
        {
            var participant = CreateApprovedParticipant(
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            Action act = () => participant.MarkOfflinePaymentAsPaid();

            act.Should().Throw<InvalidOperationException>();
        }

        private static GameParticipant CreatePendingParticipant()
        {
            return GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateApprovedParticipant(
            GameParticipantOfflinePaymentStatus offlinePaymentStatus = GameParticipantOfflinePaymentStatus.Pending)
        {
            return GameParticipant.JoinOpenGame(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: offlinePaymentStatus);
        }
    }
}