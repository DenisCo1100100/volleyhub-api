using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Commands.UpdateGame
{
    public sealed class UpdateGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldUpdateGameAndSaveChanges_WhenCurrentUserIsOrganizer()
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

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var startsAt = DateTimeOffset.UtcNow.AddDays(2);
            var endsAt = startsAt.AddHours(2);

            var command = new UpdateGameCommand(
                Id: game.Id,
                CourtId: Guid.NewGuid(),
                StartsAt: startsAt,
                EndsAt: endsAt,
                MaxPlayers: 16,
                PricePerPlayer: 20,
                RequiredLevel: GameLevel.Advanced,
                JoinPolicy: GameJoinPolicy.InviteOnly,
                Description: "Updated game");

            await handler.Handle(command, CancellationToken.None);

            updatedGame.Should().NotBeNull();
            updatedGame!.Id.Should().Be(game.Id);
            updatedGame.OrganizerId.Should().Be(organizerProfile.Id);
            updatedGame.CourtId.Should().Be(command.CourtId);
            updatedGame.StartsAt.Should().Be(command.StartsAt);
            updatedGame.EndsAt.Should().Be(command.EndsAt);
            updatedGame.MaxPlayers.Should().Be(command.MaxPlayers);
            updatedGame.PricePerPlayer.Should().Be(command.PricePerPlayer);
            updatedGame.RequiredLevel.Should().Be(command.RequiredLevel);
            updatedGame.JoinPolicy.Should().Be(command.JoinPolicy);
            updatedGame.Description.Should().Be(command.Description);
            updatedGame.Status.Should().Be(GameStatus.Open);

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

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand(Guid.NewGuid());

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

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand(Guid.NewGuid());

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

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand(Guid.NewGuid());

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

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand(gameId);

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

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand(game.Id);

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

        private static UpdateGameCommand CreateValidCommand(Guid gameId)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(2);

            return new UpdateGameCommand(
                Id: gameId,
                CourtId: Guid.NewGuid(),
                StartsAt: startsAt,
                EndsAt: startsAt.AddHours(2),
                MaxPlayers: 16,
                PricePerPlayer: 20,
                RequiredLevel: GameLevel.Advanced,
                JoinPolicy: GameJoinPolicy.InviteOnly,
                Description: "Updated game");
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