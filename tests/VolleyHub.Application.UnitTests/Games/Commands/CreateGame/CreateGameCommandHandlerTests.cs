using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Commands.CreateGame;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Commands.CreateGame
{
    public sealed class CreateGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldAddGameAndSaveChanges_WhenCurrentUserHasPlayerProfile()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            Game? addedGame = null;

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(organizerProfile);

            gameRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Game>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Game, CancellationToken>((game, _) => addedGame = game)
                .Returns(Task.CompletedTask);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new CreateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var endsAt = startsAt.AddHours(2);

            var command = new CreateGameCommand(
                CourtId: Guid.NewGuid(),
                StartsAt: startsAt,
                EndsAt: endsAt,
                MaxPlayers: 12,
                PricePerPlayer: 15,
                RequiredLevel: GameLevel.Intermediate,
                JoinPolicy: GameJoinPolicy.ApprovalRequired,
                Description: "Evening volleyball game");

            var gameId = await handler.Handle(command, CancellationToken.None);

            gameId.Should().NotBeEmpty();

            addedGame.Should().NotBeNull();
            addedGame!.Id.Should().Be(gameId);
            addedGame.OrganizerId.Should().Be(organizerProfile.Id);
            addedGame.CourtId.Should().Be(command.CourtId);
            addedGame.StartsAt.Should().Be(command.StartsAt);
            addedGame.EndsAt.Should().Be(command.EndsAt);
            addedGame.MaxPlayers.Should().Be(command.MaxPlayers);
            addedGame.PricePerPlayer.Should().Be(command.PricePerPlayer);
            addedGame.RequiredLevel.Should().Be(command.RequiredLevel);
            addedGame.JoinPolicy.Should().Be(command.JoinPolicy);
            addedGame.Description.Should().Be(command.Description);
            addedGame.Status.Should().Be(GameStatus.Open);

            gameRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Game>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new CreateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand();

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Game>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserDoesNotHavePlayerProfile()
        {
            var currentUserId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new CreateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand();

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Game>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            organizerProfile.Delete();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(organizerProfile);

            var handler = new CreateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand();

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Game>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static CreateGameCommand CreateValidCommand()
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var endsAt = startsAt.AddHours(2);

            return new CreateGameCommand(
                CourtId: Guid.NewGuid(),
                StartsAt: startsAt,
                EndsAt: endsAt,
                MaxPlayers: 12,
                PricePerPlayer: 15,
                RequiredLevel: GameLevel.Intermediate,
                JoinPolicy: GameJoinPolicy.ApprovalRequired,
                Description: "Evening volleyball game");
        }

        private static PlayerProfile CreatePlayerProfile(Guid userId)
        {
            return PlayerProfile.Create(
                userId: userId,
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "I like volleyball.");
        }
    }
}