using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameRecurrences.Commands.CancelGameRecurrence;
using VolleyHub.Application.GameRecurrences.Commands.CreateGameRecurrence;
using VolleyHub.Application.GameRecurrences.Commands.UpdateFutureGames;
using VolleyHub.Application.GameRecurrences.Queries.GetGameRecurrence;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameRecurrences
{
    public sealed class GameRecurrenceHandlerTests
    {
        private readonly Mock<IGameRecurrenceRepository> _recurrences = new();
        private readonly Mock<IGameRepository> _games = new();
        private readonly Mock<IGameParticipantRepository> _participants = new();
        private readonly Mock<IPlayerProfileRepository> _profiles = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<ICourtRepository> _courts = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IDateTimeProvider> _clock = new();
        private readonly DateTimeOffset _now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
        private readonly PlayerProfile _organizer;
        private readonly Game _source;
        private readonly GameRecurrence _recurrence;
        private readonly Game[] _occurrences;

        public GameRecurrenceHandlerTests()
        {
            _organizer = PlayerProfile.Create(Guid.NewGuid(), "Organizer", PlayerSkillLevel.Intermediate, "Minsk", null);
            var court = Court.Create(_organizer.Id, "Court", "Address", 53.9, 27.56, CourtSurfaceType.Indoor, true, null);
            _source = Game.Create(_organizer.Id, court.Id, _now.AddDays(-7), _now.AddDays(-7).AddHours(2), 12, 15,
                GameLevel.Intermediate, GameJoinPolicy.Open, "Weekly game");
            _recurrence = GameRecurrence.Create(Guid.NewGuid(), _source, _now.AddDays(1), 3, _now);
            _occurrences = Enumerable.Range(1, 3).Select(_recurrence.CreateOccurrence).ToArray();
            _clock.SetupGet(clock => clock.UtcNow).Returns(_now);
            _currentUser.SetupGet(user => user.UserId).Returns(_organizer.UserId);
            _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(_organizer);
            _courts.Setup(repository => repository.GetByIdAsync(court.Id, It.IsAny<CancellationToken>())).ReturnsAsync(court);
            _games.Setup(repository => repository.GetByIdAsync(_source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_source);
            _recurrences.Setup(repository => repository.GetByIdAsync(_recurrence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_recurrence);
            _recurrences.Setup(repository => repository.GetOccurrencesAsync(_recurrence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_occurrences);
            _participants.Setup(repository => repository.GetByGameIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<GameParticipant>());
        }

        [Fact]
        public async Task Create_ShouldPersistDefinitionAndAllGamesWithOneSave()
        {
            var command = new CreateGameRecurrenceCommand(Guid.NewGuid(), _source.Id, _now.AddDays(7), 3);
            var added = new List<Game>();
            _games.Setup(repository => repository.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
                .Callback<Game, CancellationToken>((game, _) => added.Add(game)).Returns(Task.CompletedTask);

            (await CreateHandler().Handle(command, CancellationToken.None)).Should().Be(command.Id);
            added.Should().HaveCount(3).And.OnlyContain(game => game.RecurrenceId == command.Id && game.Status == GameStatus.Open);
            added.Select(game => game.OccurrenceNumber).Should().Equal(1, 2, 3);
            _recurrences.Verify(repository => repository.AddAsync(It.Is<GameRecurrence>(r => r.Id == command.Id), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldRejectRepeatedIdentityWithoutGeneratingGames()
        {
            var act = () => CreateHandler().Handle(new(_recurrence.Id, _source.Id, _now.AddDays(7), 3), CancellationToken.None);
            await act.Should().ThrowAsync<ConflictException>();
            _games.Verify(repository => repository.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoSave();
        }

        [Theory]
        [InlineData("create")]
        [InlineData("update")]
        [InlineData("cancel")]
        [InlineData("read")]
        public async Task Operations_ShouldRequireAuthentication(string operation)
        {
            _currentUser.SetupGet(user => user.UserId).Returns((Guid?)null);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<UnauthorizedException>();
            VerifyNoSave();
        }

        [Theory]
        [InlineData("create")]
        [InlineData("update")]
        [InlineData("cancel")]
        [InlineData("read")]
        public async Task Operations_ShouldRejectOtherOrganizer(string operation)
        {
            var other = PlayerProfile.Create(_organizer.UserId, "Other", PlayerSkillLevel.Beginner, "Minsk", null);
            _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(other);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            VerifyNoSave();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Create_ShouldRequireActiveProfile(bool deleted)
        {
            if (deleted) _organizer.Delete();
            else _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((PlayerProfile?)null);
            var act = () => ExecuteAsync("create");
            await act.Should().ThrowAsync<NotFoundException>();
            VerifyNoSave();
        }

        [Fact]
        public async Task Create_ShouldRejectMissingSourceAndDeletedCourt()
        {
            var act = () => CreateHandler().Handle(new(Guid.NewGuid(), Guid.NewGuid(), _now.AddDays(1), 2), CancellationToken.None);
            await act.Should().ThrowAsync<NotFoundException>();
            _courts.Setup(repository => repository.GetByIdAsync(_source.CourtId, It.IsAny<CancellationToken>())).ReturnsAsync((Court?)null);
            await ((Func<Task>)(() => ExecuteAsync("create"))).Should().ThrowAsync<NotFoundException>();
            VerifyNoSave();
        }

        [Fact]
        public async Task Update_ShouldApplyOnlySelectedFutureSettingsAndSynchronizePayments()
        {
            var participant = GameParticipant.JoinOpenGame(_occurrences[1].Id, Guid.NewGuid(), _now, GameParticipantOfflinePaymentStatus.Pending);
            _participants.Setup(repository => repository.GetByGameIdAsync(_occurrences[1].Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { participant });
            _participants.Setup(repository => repository.GetByIdAsync(participant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(participant);
            _occurrences[2].Cancel();

            await UpdateHandler().Handle(UpdateCommand() with { FromOccurrenceNumber = 2, PricePerPlayer = 0 }, CancellationToken.None);

            _occurrences[0].Description.Should().Be("Weekly game");
            _occurrences[1].Description.Should().Be("Updated");
            _occurrences[1].StartsAt.Should().Be(_recurrence.GetStartsAt(2));
            _occurrences[2].Description.Should().Be("Weekly game");
            _recurrence.PricePerPlayer.Should().Be(0);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
            _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldRejectEntireSaveWhenAnyGameIsFull()
        {
            _occurrences[2].MarkAsFull();
            var act = () => UpdateHandler().Handle(UpdateCommand(), CancellationToken.None);
            await act.Should().ThrowAsync<BusinessRuleException>();
            VerifyNoSave();
        }

        [Fact]
        public async Task Update_ShouldProtectRecordedPayments()
        {
            var participant = GameParticipant.JoinOpenGame(_occurrences[1].Id, Guid.NewGuid(), _now, GameParticipantOfflinePaymentStatus.Pending);
            participant.UpdateOfflinePaymentStatus(_occurrences[1], GameParticipantOfflinePaymentStatus.Paid);
            _participants.Setup(repository => repository.GetByGameIdAsync(_occurrences[1].Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { participant });
            var act = () => UpdateHandler().Handle(UpdateCommand() with { PricePerPlayer = 20 }, CancellationToken.None);
            await act.Should().ThrowAsync<BusinessRuleException>();
            VerifyNoSave();
        }

        [Fact]
        public async Task Update_ShouldRejectMissingCourtAndEmptyFutureRange()
        {
            var missingCourt = () => UpdateHandler().Handle(UpdateCommand() with { CourtId = Guid.NewGuid() }, CancellationToken.None);
            await missingCourt.Should().ThrowAsync<NotFoundException>();
            _clock.SetupGet(clock => clock.UtcNow).Returns(_now.AddYears(1));
            var empty = () => UpdateHandler().Handle(UpdateCommand(), CancellationToken.None);
            await empty.Should().ThrowAsync<BusinessRuleException>();
            VerifyNoSave();
        }

        [Fact]
        public async Task Cancel_ShouldKeepStartedAndCompletedGamesAndBeIdempotent()
        {
            _clock.SetupGet(clock => clock.UtcNow).Returns(_occurrences[0].StartsAt);
            _occurrences[1].Complete();
            _occurrences[2].MarkAsFull();
            await CancelHandler().Handle(new(_recurrence.Id), CancellationToken.None);
            await CancelHandler().Handle(new(_recurrence.Id), CancellationToken.None);

            _occurrences.Select(game => game.Status).Should().Equal(GameStatus.Open, GameStatus.Completed, GameStatus.Cancelled);
            _recurrence.CancelledAt.Should().Be(_occurrences[0].StartsAt);
            _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Read_ShouldReturnOrderedOccurrenceIdentitiesAndOriginalDates()
        {
            var game = _occurrences[0];
            game.UpdateDetails(game.OrganizerId, game.CourtId, _now.AddDays(2), null, 12, 15, GameLevel.Any, GameJoinPolicy.Open, null);
            var result = await ReadHandler().Handle(new(_recurrence.Id), CancellationToken.None);
            result.Occurrences.Select(item => item.OccurrenceNumber).Should().Equal(1, 2, 3);
            result.Occurrences[0].ScheduledStartsAt.Should().Be(_now.AddDays(1));
            result.Occurrences[0].StartsAt.Should().Be(_now.AddDays(2));
        }

        private void VerifyNoSave() => _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        private CreateGameRecurrenceCommandHandler CreateHandler() => new(_recurrences.Object, _profiles.Object, _currentUser.Object, _games.Object, _courts.Object, _unitOfWork.Object, _clock.Object);
        private UpdateFutureGamesCommandHandler UpdateHandler() => new(_recurrences.Object, _profiles.Object, _currentUser.Object, _games.Object, _participants.Object, _courts.Object, _unitOfWork.Object, _clock.Object);
        private CancelGameRecurrenceCommandHandler CancelHandler() => new(_recurrences.Object, _profiles.Object, _currentUser.Object, _games.Object, _unitOfWork.Object, _clock.Object);
        private GetGameRecurrenceQueryHandler ReadHandler() => new(_recurrences.Object, _profiles.Object, _currentUser.Object);
        private UpdateFutureGamesCommand UpdateCommand() => new(_recurrence.Id, 1, _source.CourtId, 12, 15, GameLevel.Any, GameJoinPolicy.Open, "Updated");

        private async Task ExecuteAsync(string operation)
        {
            switch (operation)
            {
                case "create": await CreateHandler().Handle(new(Guid.NewGuid(), _source.Id, _now.AddDays(7), 3), CancellationToken.None); break;
                case "update": await UpdateHandler().Handle(UpdateCommand(), CancellationToken.None); break;
                case "cancel": await CancelHandler().Handle(new(_recurrence.Id), CancellationToken.None); break;
                case "read": await ReadHandler().Handle(new(_recurrence.Id), CancellationToken.None); break;
                default: throw new ArgumentException("Unknown operation.", nameof(operation));
            }
        }
    }
}
