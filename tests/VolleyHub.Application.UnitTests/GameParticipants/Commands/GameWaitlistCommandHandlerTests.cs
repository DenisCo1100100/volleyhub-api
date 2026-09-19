using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.ApproveParticipant;
using VolleyHub.Application.GameParticipants.Commands.JoinGame;
using VolleyHub.Application.GameParticipants.Commands.JoinGameWaitlist;
using VolleyHub.Application.GameParticipants.Commands.LeaveGame;
using VolleyHub.Application.GameParticipants.Commands.PromoteGameWaitlist;
using VolleyHub.Application.GameParticipants.Commands.RejectParticipant;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands
{
    public sealed class GameWaitlistCommandHandlerTests
    {
        private readonly Mock<IGameRepository> _games = new();
        private readonly Mock<IGameParticipantRepository> _participants = new();
        private readonly Mock<IPlayerProfileRepository> _profiles = new();
        private readonly Mock<ICurrentUserService> _user = new();
        private readonly Mock<IDateTimeProvider> _clock = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly List<GameParticipant> _entries = [];
        private readonly DateTimeOffset _now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        private readonly PlayerProfile _organizer;
        private readonly Game _game;

        public GameWaitlistCommandHandlerTests()
        {
            _organizer = CreateProfile();
            _game = Game.Create(_organizer.Id, Guid.NewGuid(), _now.AddDays(2), null, 2, 15, GameLevel.Intermediate, GameJoinPolicy.Open, null);
            _game.MarkAsFull();
            _games.Setup(repository => repository.GetByIdAsync(_game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_game);
            _participants.Setup(repository => repository.GetByGameIdAsync(_game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_entries);
            _participants.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => _entries.SingleOrDefault(participant => participant.Id == id));
            _participants.Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(_game.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, Guid profileId, CancellationToken _) => _entries.SingleOrDefault(participant => participant.PlayerProfileId == profileId));
            _clock.SetupGet(clock => clock.UtcNow).Returns(_now);
            SignIn(_organizer);
        }

        [Fact]
        public async Task Join_ShouldPersistWaitlistedPlayerWithoutChangingCapacity()
        {
            AddApproved();
            AddApproved();
            var player = CreateProfile();
            SignIn(player);
            GameParticipant? added = null;
            _participants.Setup(repository => repository.AddAsync(It.IsAny<GameParticipant>(), It.IsAny<CancellationToken>()))
                .Callback<GameParticipant, CancellationToken>((participant, _) => added = participant).Returns(Task.CompletedTask);

            var id = await JoinHandler().Handle(new(_game.Id), CancellationToken.None);

            added.Should().NotBeNull();
            added!.Id.Should().Be(id);
            added.PlayerProfileId.Should().Be(player.Id);
            added.JoinStatus.Should().Be(GameParticipantJoinStatus.Waitlisted);
            _game.Status.Should().Be(GameStatus.Full);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(GameParticipantJoinStatus.Waitlisted)]
        [InlineData(GameParticipantJoinStatus.Approved)]
        [InlineData(GameParticipantJoinStatus.Cancelled)]
        [InlineData(GameParticipantJoinStatus.Rejected)]
        [InlineData(GameParticipantJoinStatus.PendingApproval)]
        [InlineData(GameParticipantJoinStatus.Removed)]
        public async Task Join_ShouldRejectEveryExistingParticipationAttempt(GameParticipantJoinStatus status)
        {
            var player = CreateProfile();
            SignIn(player);
            var participant = GameParticipant.JoinWaitlist(_game, player.Id, _now, 2);
            switch (status)
            {
                case GameParticipantJoinStatus.Cancelled: participant.WithdrawFromWaitlist(); break;
                case GameParticipantJoinStatus.Rejected: participant.RejectFromWaitlist(); break;
                case GameParticipantJoinStatus.PendingApproval:
                    participant = GameParticipant.RequestToJoin(_game.Id, player.Id, _now, GameParticipantOfflinePaymentStatus.Pending); break;
                case GameParticipantJoinStatus.Approved:
                case GameParticipantJoinStatus.Removed:
                    participant = GameParticipant.JoinOpenGame(_game.Id, player.Id, _now, GameParticipantOfflinePaymentStatus.Pending);
                    if (status is GameParticipantJoinStatus.Removed) participant.Remove(_now, _game.StartsAt);
                    break;
            }
            _entries.Add(participant);

            Func<Task> act = () => JoinHandler().Handle(new(_game.Id), CancellationToken.None);
            await act.Should().ThrowAsync<BusinessRuleException>();
            VerifyNoSave();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Commands_ShouldRequireAuthentication(bool promote)
        {
            _user.SetupGet(user => user.UserId).Returns((Guid?)null);
            Func<Task> act = () => Execute(promote);
            await act.Should().ThrowAsync<UnauthorizedException>();
            VerifyNoSave();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Commands_ShouldRequireActiveProfile(bool promote, bool deleted)
        {
            if (deleted) _organizer.Delete();
            else _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((PlayerProfile?)null);

            Func<Task> act = () => Execute(promote);
            await act.Should().ThrowAsync<NotFoundException>();
            VerifyNoSave();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Commands_ShouldReturnNotFound_WhenGameMissing(bool promote)
        {
            _games.Setup(repository => repository.GetByIdAsync(_game.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Game?)null);
            Func<Task> act = () => Execute(promote);
            await act.Should().ThrowAsync<NotFoundException>();
            VerifyNoSave();
        }

        [Fact]
        public async Task Promote_ShouldRequireOrganizerOwnership()
        {
            SignIn(CreateProfile());
            Func<Task> act = () => Execute(true);
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            VerifyNoSave();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Promote_ShouldChooseOldestThenId_RegardlessOfRepositoryOrder(bool equalTimestamps)
        {
            var first = AddWaitlisted(_now.AddMinutes(-2));
            var second = AddWaitlisted(equalTimestamps ? first.JoinedAt : _now.AddMinutes(-1));
            var expected = _entries.OrderBy(participant => participant.JoinedAt).ThenBy(participant => participant.Id).First();
            _entries.Reverse();
            AddApproved();
            _game.Reopen();

            var id = await PromoteHandler().Handle(new(_game.Id), CancellationToken.None);

            id.Should().Be(expected.Id);
            expected.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            _entries.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved).Should().Be(2);
            _entries.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Waitlisted).Should().Be(1);
            _game.Status.Should().Be(GameStatus.Full);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Promote_ShouldSkipWithdrawnRejectedAndDeletedProfiles()
        {
            AddWaitlisted(_now.AddMinutes(-4)).WithdrawFromWaitlist();
            AddWaitlisted(_now.AddMinutes(-3)).RejectFromWaitlist();
            var deletedProfile = CreateProfile();
            AddWaitlisted(_now.AddMinutes(-2), deletedProfile);
            deletedProfile.Delete();
            var eligible = AddWaitlisted(_now.AddMinutes(-1));
            _game.Reopen();

            var id = await PromoteHandler().Handle(new(_game.Id), CancellationToken.None);
            id.Should().Be(eligible.Id);
        }

        [Fact]
        public async Task Promote_ShouldRejectEmptyQueueAndFullGame()
        {
            Func<Task> act = () => Execute(true);
            await act.Should().ThrowAsync<BusinessRuleException>();
            _game.Reopen();
            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("There are no eligible players on the waitlist.");
            VerifyNoSave();
        }

        [Fact]
        public async Task Leave_ShouldWithdrawWaitlistedPlayerWithoutReopeningGame()
        {
            var player = CreateProfile();
            var participant = AddWaitlisted(_now, player);
            SignIn(player);
            var handler = new LeaveGameCommandHandler(_games.Object, _participants.Object, _profiles.Object, _user.Object, _clock.Object, _unitOfWork.Object);

            await handler.Handle(new(_game.Id), CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
            _game.Status.Should().Be(GameStatus.Full);
        }

        [Fact]
        public async Task Reject_ShouldAllowOrganizerToDismissWaitlistedPlayer()
        {
            var participant = AddWaitlisted(_now);
            var handler = new RejectParticipantCommandHandler(_games.Object, _participants.Object, _profiles.Object, _user.Object, _unitOfWork.Object);

            await handler.Handle(new(participant.Id), CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Rejected);
            _game.Status.Should().Be(GameStatus.Full);
        }

        [Fact]
        public async Task JoinAndApprove_ShouldNotBypassWaitlist()
        {
            AddWaitlisted(_now);
            var pending = GameParticipant.RequestToJoin(_game.Id, Guid.NewGuid(), _now, GameParticipantOfflinePaymentStatus.Pending);
            _entries.Add(pending);
            _game.Reopen();
            var join = new JoinGameCommandHandler(_games.Object, _participants.Object, _profiles.Object, _user.Object, _clock.Object, _unitOfWork.Object);
            var approve = new ApproveParticipantCommandHandler(_games.Object, _participants.Object, _profiles.Object, _user.Object, _clock.Object, _unitOfWork.Object);

            Func<Task> joinAction = () => join.Handle(new(_game.Id), CancellationToken.None);
            Func<Task> approveAction = () => approve.Handle(new(pending.Id), CancellationToken.None);
            await joinAction.Should().ThrowAsync<BusinessRuleException>().WithMessage("*waitlist first*");
            await approveAction.Should().ThrowAsync<BusinessRuleException>().WithMessage("*waitlist first*");
            VerifyNoSave();
        }

        [Fact]
        public void Validators_ShouldRejectEmptyGameIds()
        {
            new JoinGameWaitlistCommandValidator().Validate(new JoinGameWaitlistCommand(Guid.Empty)).IsValid.Should().BeFalse();
            new PromoteGameWaitlistCommandValidator().Validate(new PromoteGameWaitlistCommand(Guid.Empty)).IsValid.Should().BeFalse();
            new JoinGameWaitlistCommandValidator().Validate(new JoinGameWaitlistCommand(_game.Id)).IsValid.Should().BeTrue();
            new PromoteGameWaitlistCommandValidator().Validate(new PromoteGameWaitlistCommand(_game.Id)).IsValid.Should().BeTrue();
        }

        private Task<Guid> Execute(bool promote) => promote
            ? PromoteHandler().Handle(new(_game.Id), CancellationToken.None)
            : JoinHandler().Handle(new(_game.Id), CancellationToken.None);

        private JoinGameWaitlistCommandHandler JoinHandler() => new(_games.Object, _participants.Object, _profiles.Object, _user.Object, _clock.Object, _unitOfWork.Object);
        private PromoteGameWaitlistCommandHandler PromoteHandler() => new(_games.Object, _participants.Object, _profiles.Object, _user.Object, _clock.Object, _unitOfWork.Object);
        private void VerifyNoSave() => _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        private void SignIn(PlayerProfile profile)
        {
            _user.SetupGet(user => user.UserId).Returns(profile.UserId);
            _profiles.Setup(repository => repository.GetByUserIdAsync(profile.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        }

        private void AddApproved() => _entries.Add(GameParticipant.JoinOpenGame(_game.Id, Guid.NewGuid(), _now, GameParticipantOfflinePaymentStatus.Pending));

        private GameParticipant AddWaitlisted(DateTimeOffset joinedAt, PlayerProfile? profile = null)
        {
            profile ??= CreateProfile();
            _profiles.Setup(repository => repository.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
            var participant = GameParticipant.JoinWaitlist(_game, profile.Id, joinedAt, 2);
            _entries.Add(participant);
            return participant;
        }

        private static PlayerProfile CreateProfile() => PlayerProfile.Create(Guid.NewGuid(), "Waitlist Player", PlayerSkillLevel.Intermediate, "Minsk", null);
    }
}
