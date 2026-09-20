using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.UpdateParticipantOfflinePayment;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Application.Games.Queries.GetGameOfflinePaymentSummary;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands
{
    public sealed class OfflinePaymentHandlerTests
    {
        private readonly Mock<IGameRepository> _games = new();
        private readonly Mock<IGameParticipantRepository> _participants = new();
        private readonly Mock<IPlayerProfileRepository> _profiles = new();
        private readonly Mock<ICurrentUserService> _user = new();
        private readonly Mock<IUnitOfWork> _unit = new();
        private readonly PlayerProfile _organizer;
        private readonly Game _game;
        private readonly GameParticipant _participant;

        public OfflinePaymentHandlerTests()
        {
            _organizer = PlayerProfile.Create(Guid.NewGuid(), "Organizer", PlayerSkillLevel.Intermediate, "Minsk", null);
            _game = Game.Create(_organizer.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(2), null, 12, 12.35m,
                GameLevel.Any, GameJoinPolicy.Open, null);
            _participant = GameParticipant.JoinOpenGame(_game.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1), GameParticipantOfflinePaymentStatus.Pending);
            _user.SetupGet(user => user.UserId).Returns(_organizer.UserId);
            _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(_organizer);
            _games.Setup(repository => repository.GetByIdAsync(_game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_game);
            _participants.Setup(repository => repository.GetByIdAsync(_participant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_participant);
            _participants.Setup(repository => repository.GetByGameIdAsync(_game.Id, It.IsAny<CancellationToken>())).ReturnsAsync([_participant]);
        }

        [Fact]
        public async Task Update_ShouldSaveOrganizerPaymentAndCorrection()
        {
            await UpdateAsync(GameParticipantOfflinePaymentStatus.Paid);
            _participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            await UpdateAsync(GameParticipantOfflinePaymentStatus.Pending);
            _participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
            _participants.Verify(repository => repository.Update(_participant), Times.Exactly(2));
            _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missingProfile")]
        [InlineData("deletedProfile")]
        [InlineData("otherOrganizer")]
        [InlineData("missingGame")]
        public async Task UpdateAndSummary_ShouldEnforceAccess(string scenario)
        {
            var exceptionType = ConfigureAccessFailure(scenario);
            Func<Task> update = () => UpdateAsync();
            Func<Task> query = () => SummaryAsync();
            (await update.Should().ThrowAsync<Exception>()).Which.GetType().Should().Be(exceptionType);
            (await query.Should().ThrowAsync<Exception>()).Which.GetType().Should().Be(exceptionType);
            _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _participants.Verify(repository => repository.GetByGameIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
        }

        [Fact]
        public async Task Update_ShouldRejectMissingParticipantWithoutSaving()
        {
            _participants.Setup(repository => repository.GetByIdAsync(_participant.Id, It.IsAny<CancellationToken>())).ReturnsAsync((GameParticipant?)null);
            Func<Task> act = () => UpdateAsync();
            await act.Should().ThrowAsync<NotFoundException>();
            _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_ShouldRejectCancelledParticipationWithoutSaving()
        {
            _participant.CancelParticipation(DateTimeOffset.UtcNow, _game.StartsAt);
            Func<Task> act = () => UpdateAsync();
            await act.Should().ThrowAsync<BusinessRuleException>();
            _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Summary_ShouldCountOnlyConfirmedPlacesAndUseExactDecimalAmounts()
        {
            var now = DateTimeOffset.UtcNow;
            var pending = GameParticipant.RequestToJoin(_game.Id, Guid.NewGuid(), now, GameParticipantOfflinePaymentStatus.Pending);
            var rejected = GameParticipant.RequestToJoin(_game.Id, Guid.NewGuid(), now, GameParticipantOfflinePaymentStatus.Pending);
            rejected.Reject();
            var cancelled = GameParticipant.JoinOpenGame(_game.Id, Guid.NewGuid(), now, GameParticipantOfflinePaymentStatus.Paid);
            cancelled.CancelParticipation(now, _game.StartsAt);
            var removed = GameParticipant.JoinOpenGame(_game.Id, Guid.NewGuid(), now, GameParticipantOfflinePaymentStatus.Paid);
            removed.Remove(now, _game.StartsAt);
            var paid = GameParticipant.JoinOpenGame(_game.Id, Guid.NewGuid(), now, GameParticipantOfflinePaymentStatus.Paid);
            _game.MarkAsFull();
            var waitlisted = GameParticipant.JoinWaitlist(_game, Guid.NewGuid(), now, _game.MaxPlayers);
            _game.Reopen();
            _participants.Setup(repository => repository.GetByGameIdAsync(_game.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([_participant, pending, rejected, cancelled, removed, paid, waitlisted]);

            var summary = await SummaryAsync();

            summary.ExpectedParticipantCount.Should().Be(2);
            summary.PaidParticipantCount.Should().Be(1);
            summary.OutstandingParticipantCount.Should().Be(1);
            summary.ExpectedAmount.Should().Be(24.70m);
            summary.PaidAmount.Should().Be(12.35m);
            summary.OutstandingAmount.Should().Be(12.35m);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Summary_ShouldReturnZeroForFreeOrCancelledGames(bool free)
        {
            if (free) SetPrice(0);
            else _game.Cancel();
            var summary = await SummaryAsync();
            summary.ExpectedParticipantCount.Should().Be(0);
            summary.PaidParticipantCount.Should().Be(0);
            summary.OutstandingAmount.Should().Be(0);
        }

        [Fact]
        public async Task UpdateGame_ShouldSynchronizePaymentRequirement()
        {
            await UpdateGameAsync(0);
            _participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
            await UpdateGameAsync(15);
            _participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
            _participants.Verify(repository => repository.Update(_participant), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateGame_ShouldRejectPriceChangeWithRecordedPaymentBeforeMutatingGame()
        {
            _participant.UpdateOfflinePaymentStatus(_game, GameParticipantOfflinePaymentStatus.Paid);
            Func<Task> act = () => UpdateGameAsync(30);
            await act.Should().ThrowAsync<BusinessRuleException>();
            _game.PricePerPlayer.Should().Be(12.35m);
            _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(GameParticipantOfflinePaymentStatus.Unknown, false)]
        [InlineData((GameParticipantOfflinePaymentStatus)999, false)]
        [InlineData(GameParticipantOfflinePaymentStatus.NotRequired, true)]
        [InlineData(GameParticipantOfflinePaymentStatus.Pending, true)]
        [InlineData(GameParticipantOfflinePaymentStatus.Paid, true)]
        public void Validator_ShouldValidateStatusAndParticipantId(GameParticipantOfflinePaymentStatus status, bool valid)
        {
            var validator = new UpdateParticipantOfflinePaymentCommandValidator();
            validator.Validate(new UpdateParticipantOfflinePaymentCommand(_participant.Id, status)).IsValid.Should().Be(valid);
            validator.Validate(new UpdateParticipantOfflinePaymentCommand(Guid.Empty, status)).IsValid.Should().BeFalse();
            new GetGameOfflinePaymentSummaryQueryValidator().Validate(new GetGameOfflinePaymentSummaryQuery(Guid.Empty)).IsValid.Should().BeFalse();
        }

        private Type ConfigureAccessFailure(string scenario)
        {
            switch (scenario)
            {
                case "anonymous":
                    _user.SetupGet(user => user.UserId).Returns((Guid?)null);
                    return typeof(UnauthorizedException);
                case "missingProfile":
                    _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((PlayerProfile?)null);
                    return typeof(NotFoundException);
                case "deletedProfile":
                    _organizer.Delete();
                    return typeof(NotFoundException);
                case "otherOrganizer":
                    _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(PlayerProfile.Create(_organizer.UserId, "Other", PlayerSkillLevel.Intermediate, null, null));
                    return typeof(ForbiddenAccessException);
                default:
                    _games.Setup(repository => repository.GetByIdAsync(_game.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Game?)null);
                    return typeof(NotFoundException);
            }
        }

        private Task UpdateAsync(GameParticipantOfflinePaymentStatus status = GameParticipantOfflinePaymentStatus.Paid) =>
            new UpdateParticipantOfflinePaymentCommandHandler(_games.Object, _participants.Object, _profiles.Object, _user.Object, _unit.Object)
                .Handle(new UpdateParticipantOfflinePaymentCommand(_participant.Id, status), CancellationToken.None);

        private Task<VolleyHub.Application.Games.Common.GameOfflinePaymentSummaryDto> SummaryAsync() =>
            new GetGameOfflinePaymentSummaryQueryHandler(_games.Object, _participants.Object, _profiles.Object, _user.Object)
                .Handle(new GetGameOfflinePaymentSummaryQuery(_game.Id), CancellationToken.None);

        private Task UpdateGameAsync(decimal price) =>
            new UpdateGameCommandHandler(_games.Object, _profiles.Object, _user.Object, _unit.Object, _participants.Object)
                .Handle(new UpdateGameCommand(_game.Id, _game.CourtId, _game.StartsAt, _game.EndsAt, _game.MaxPlayers,
                    price, _game.RequiredLevel, _game.JoinPolicy, _game.Description), CancellationToken.None);

        private void SetPrice(decimal price) => _game.UpdateDetails(_game.OrganizerId, _game.CourtId, _game.StartsAt, _game.EndsAt,
            _game.MaxPlayers, price, _game.RequiredLevel, _game.JoinPolicy, _game.Description);
    }
}
