using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Commands.CreateGameFromTemplate;
using VolleyHub.Application.GameTemplates.Commands.CreateGameTemplate;
using VolleyHub.Application.GameTemplates.Commands.DeleteGameTemplate;
using VolleyHub.Application.GameTemplates.Commands.UpdateGameTemplate;
using VolleyHub.Application.GameTemplates.Queries.GetGameTemplateById;
using VolleyHub.Application.GameTemplates.Queries.GetGameTemplates;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameTemplates
{
    public sealed class GameTemplateHandlerTests
    {
        private readonly Mock<IGameTemplateRepository> _templates = new();
        private readonly Mock<IGameRepository> _games = new();
        private readonly Mock<IPlayerProfileRepository> _profiles = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<ICourtRepository> _courts = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly PlayerProfile _organizer;
        private readonly Court _court;
        private readonly GameTemplate _template;

        public GameTemplateHandlerTests()
        {
            _organizer = PlayerProfile.Create(Guid.NewGuid(), "Organizer", PlayerSkillLevel.Intermediate, "Minsk", null);
            _court = Court.Create(Guid.NewGuid(), "Court", "Address", 53.9, 27.56, CourtSurfaceType.Indoor, true, null);
            _template = GameTemplate.Create(_organizer.Id, "Practice", _court.Id, TimeSpan.FromHours(2), 12, 25,
                GameLevel.Intermediate, GameJoinPolicy.ApprovalRequired, "Evening game");
            _currentUser.SetupGet(user => user.UserId).Returns(_organizer.UserId);
            _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(_organizer);
            _courts.Setup(repository => repository.GetByIdAsync(_court.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_court);
            _templates.Setup(repository => repository.GetByIdAsync(_template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_template);
            _templates.Setup(repository => repository.GetByOrganizerIdAsync(_organizer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { _template });
        }

        [Fact]
        public async Task Create_ShouldUseCurrentProfileAndPersistWithoutCreatingGame()
        {
            GameTemplate? added = null;
            _templates.Setup(repository => repository.AddAsync(It.IsAny<GameTemplate>(), It.IsAny<CancellationToken>()))
                .Callback<GameTemplate, CancellationToken>((template, _) => added = template).Returns(Task.CompletedTask);

            var id = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

            added.Should().NotBeNull();
            added!.Id.Should().Be(id);
            added.OrganizerId.Should().Be(_organizer.Id);
            added.CourtId.Should().Be(_court.Id);
            added.Duration.Should().Be(TimeSpan.FromHours(2));
            added.Name.Should().Be("Practice");
            _games.Verify(repository => repository.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyOneSave();
        }

        [Fact]
        public async Task Update_ShouldPersistNewSettingsAndKeepOwner()
        {
            await UpdateHandler().Handle(UpdateCommand() with { Name = "Free practice", Duration = null, PricePerPlayer = 0 }, CancellationToken.None);

            _template.Name.Should().Be("Free practice");
            _template.Duration.Should().BeNull();
            _template.PricePerPlayer.Should().Be(0);
            _template.OrganizerId.Should().Be(_organizer.Id);
            _templates.Verify(repository => repository.Update(_template), Times.Once);
            _games.VerifyNoOtherCalls();
            VerifyOneSave();
        }

        [Fact]
        public async Task Delete_ShouldRemoveOnlyOwnedTemplateWithoutRequiringActiveCourt()
        {
            _court.Delete();
            await DeleteHandler().Handle(new(_template.Id), CancellationToken.None);
            _templates.Verify(repository => repository.Delete(_template), Times.Once);
            _games.VerifyNoOtherCalls();
            _courts.VerifyNoOtherCalls();
            VerifyOneSave();
        }

        [Fact]
        public async Task Use_ShouldCreateIndependentGameWithRuntimeScheduleInOneSave()
        {
            Game? added = null;
            var startsAt = DateTimeOffset.UtcNow.AddDays(1).ToOffset(TimeSpan.FromHours(3));
            _games.Setup(repository => repository.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
                .Callback<Game, CancellationToken>((game, _) => added = game).Returns(Task.CompletedTask);

            var id = await UseHandler().Handle(new(_template.Id, startsAt), CancellationToken.None);

            added.Should().NotBeNull();
            added!.Id.Should().Be(id);
            added.OrganizerId.Should().Be(_organizer.Id);
            added.CourtId.Should().Be(_court.Id);
            added.StartsAt.Should().Be(startsAt);
            added.StartsAt.Offset.Should().Be(TimeSpan.Zero);
            added.EndsAt.Should().Be(startsAt.AddHours(2));
            added.RecurrenceId.Should().BeNull();
            added.Status.Should().Be(GameStatus.Open);
            _templates.Verify(repository => repository.Update(It.IsAny<GameTemplate>()), Times.Never);
            VerifyOneSave();
        }

        [Fact]
        public async Task ReadAndList_ShouldReturnPrivateSettingsEvenIfCourtWasDeleted()
        {
            _court.Delete();
            var result = await ReadHandler().Handle(new(_template.Id), CancellationToken.None);
            var list = await ListHandler().Handle(new(), CancellationToken.None);
            result.Id.Should().Be(_template.Id);
            result.Name.Should().Be(_template.Name);
            list.Should().ContainSingle().Which.Should().Be(result);
            _templates.Verify(repository => repository.GetByOrganizerIdAsync(_organizer.Id, It.IsAny<CancellationToken>()), Times.Once);
            VerifyNoWrites();
        }

        [Theory]
        [InlineData("create")]
        [InlineData("update")]
        [InlineData("delete")]
        [InlineData("use")]
        [InlineData("read")]
        [InlineData("list")]
        public async Task Operations_ShouldRequireAuthentication(string operation)
        {
            _currentUser.SetupGet(user => user.UserId).Returns((Guid?)null);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<UnauthorizedException>();
            VerifyNoWrites();
            _templates.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("create", false)]
        [InlineData("create", true)]
        [InlineData("update", false)]
        [InlineData("update", true)]
        [InlineData("delete", false)]
        [InlineData("delete", true)]
        [InlineData("use", false)]
        [InlineData("use", true)]
        [InlineData("read", false)]
        [InlineData("read", true)]
        [InlineData("list", false)]
        [InlineData("list", true)]
        public async Task Operations_ShouldRequireActiveProfile(string operation, bool deleted)
        {
            if (deleted) _organizer.Delete();
            else _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((PlayerProfile?)null);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<NotFoundException>();
            VerifyNoWrites();
            _templates.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("update")]
        [InlineData("delete")]
        [InlineData("use")]
        [InlineData("read")]
        public async Task Operations_ShouldRejectOtherOwner(string operation)
        {
            var other = PlayerProfile.Create(_organizer.UserId, "Other", PlayerSkillLevel.Beginner, "Brest", null);
            _profiles.Setup(repository => repository.GetByUserIdAsync(_organizer.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(other);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            VerifyNoWrites();
            _courts.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("update")]
        [InlineData("delete")]
        [InlineData("use")]
        [InlineData("read")]
        public async Task Operations_ShouldRejectMissingTemplate(string operation)
        {
            _templates.Setup(repository => repository.GetByIdAsync(_template.Id, It.IsAny<CancellationToken>())).ReturnsAsync((GameTemplate?)null);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<NotFoundException>();
            VerifyNoWrites();
        }

        [Theory]
        [InlineData("create", false)]
        [InlineData("create", true)]
        [InlineData("update", false)]
        [InlineData("update", true)]
        [InlineData("use", false)]
        [InlineData("use", true)]
        public async Task Writes_ShouldRejectMissingOrDeletedCourt(string operation, bool deleted)
        {
            if (deleted) _court.Delete();
            else _courts.Setup(repository => repository.GetByIdAsync(_court.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Court?)null);
            var act = () => ExecuteAsync(operation);
            await act.Should().ThrowAsync<NotFoundException>();
            VerifyNoWrites();
            _template.Name.Should().Be("Practice");
        }

        [Fact]
        public async Task Use_ShouldNotPersistGameWithInvalidSchedule()
        {
            var missing = () => UseHandler().Handle(new(_template.Id, default), CancellationToken.None);
            var overflow = () => UseHandler().Handle(new(_template.Id, DateTimeOffset.MaxValue), CancellationToken.None);
            await missing.Should().ThrowAsync<ArgumentException>();
            await overflow.Should().ThrowAsync<ArgumentOutOfRangeException>();
            VerifyNoWrites();
        }

        private void VerifyNoWrites()
        {
            _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _games.Verify(repository => repository.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
            _templates.Verify(repository => repository.AddAsync(It.IsAny<GameTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
            _templates.Verify(repository => repository.Update(It.IsAny<GameTemplate>()), Times.Never);
            _templates.Verify(repository => repository.Delete(It.IsAny<GameTemplate>()), Times.Never);
        }

        private void VerifyOneSave() => _unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        private CreateGameTemplateCommandHandler CreateHandler() => new(_templates.Object, _profiles.Object, _currentUser.Object, _courts.Object, _unitOfWork.Object);
        private UpdateGameTemplateCommandHandler UpdateHandler() => new(_templates.Object, _profiles.Object, _currentUser.Object, _courts.Object, _unitOfWork.Object);
        private DeleteGameTemplateCommandHandler DeleteHandler() => new(_templates.Object, _profiles.Object, _currentUser.Object, _unitOfWork.Object);
        private CreateGameFromTemplateCommandHandler UseHandler() => new(_templates.Object, _profiles.Object, _currentUser.Object, _courts.Object, _games.Object, _unitOfWork.Object);
        private GetGameTemplateByIdQueryHandler ReadHandler() => new(_templates.Object, _profiles.Object, _currentUser.Object);
        private GetGameTemplatesQueryHandler ListHandler() => new(_templates.Object, _profiles.Object, _currentUser.Object);
        private CreateGameTemplateCommand CreateCommand() => new("Practice", _court.Id, TimeSpan.FromHours(2), 12, 25, GameLevel.Intermediate, GameJoinPolicy.ApprovalRequired, "Evening game");
        private UpdateGameTemplateCommand UpdateCommand() => new(_template.Id, "Updated", _court.Id, null, 6, 0, GameLevel.Any, GameJoinPolicy.Open, null);

        private async Task ExecuteAsync(string operation)
        {
            switch (operation)
            {
                case "create": await CreateHandler().Handle(CreateCommand(), CancellationToken.None); break;
                case "update": await UpdateHandler().Handle(UpdateCommand(), CancellationToken.None); break;
                case "delete": await DeleteHandler().Handle(new(_template.Id), CancellationToken.None); break;
                case "use": await UseHandler().Handle(new(_template.Id, DateTimeOffset.UtcNow.AddDays(1)), CancellationToken.None); break;
                case "read": await ReadHandler().Handle(new(_template.Id), CancellationToken.None); break;
                case "list": await ListHandler().Handle(new(), CancellationToken.None); break;
                default: throw new ArgumentException("Unknown operation.", nameof(operation));
            }
        }
    }
}
