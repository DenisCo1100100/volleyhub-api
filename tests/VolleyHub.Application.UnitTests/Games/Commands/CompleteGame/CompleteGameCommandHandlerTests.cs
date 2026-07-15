using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Commands.CompleteGame;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Commands.CompleteGame
{
    public sealed class CompleteGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCompleteGameAndSaveChanges_WhenCurrentUserIsOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame(organizerProfile.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            Game? updatedGame = null;

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(organizerProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameRepositoryMock
                .Setup(repository => repository.Update(It.IsAny<Game>()))
                .Callback<Game>(updated => updatedGame = updated);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new CompleteGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new CompleteGameCommand(game.Id);

            await handler.Handle(command, CancellationToken.None);

            updatedGame.Should().NotBeNull();
            updatedGame!.Id.Should().Be(game.Id);
            updatedGame.Status.Should().Be(GameStatus.Completed);

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
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

            var handler = new CompleteGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new CompleteGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
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

            var handler = new CompleteGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new CompleteGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
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

            var handler = new CompleteGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new CompleteGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var organizerProfile = CreatePlayerProfile(currentUserId);
            var gameId = Guid.NewGuid();

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

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new CompleteGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new CompleteGameCommand(gameId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenCurrentUserIsNotOrganizer()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserProfile = CreatePlayerProfile(currentUserId);
            var anotherOrganizerProfileId = Guid.NewGuid();
            var game = CreateGame(anotherOrganizerProfileId);

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
                .ReturnsAsync(currentUserProfile);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            var handler = new CompleteGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new CompleteGameCommand(game.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Game CreateGame(Guid organizerId)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return Game.Create(
                organizerId: organizerId,
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: 12,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Evening volleyball game");
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