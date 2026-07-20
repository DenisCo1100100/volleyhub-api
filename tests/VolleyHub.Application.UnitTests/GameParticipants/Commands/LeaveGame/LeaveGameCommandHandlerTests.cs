using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.LeaveGame;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.LeaveGame
{
    public sealed class LeaveGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCancelParticipantAndSaveChanges_WhenCurrentUserParticipantExists()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame();
            var participant = CreateApprovedParticipant(game.Id, playerProfile.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(game.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReopenGame_WhenApprovedParticipantLeavesFullGame()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame();
            game.MarkAsFull();

            var participant = CreateApprovedParticipant(game.Id, playerProfile.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(game.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            game.Status.Should().Be(GameStatus.Open);

            gameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameAndPlayerProfileIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserDoesNotHavePlayerProfile()
        {
            var currentUserId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile: null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameAndPlayerProfileIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            playerProfile.Delete();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(Guid.NewGuid());

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameAndPlayerProfileIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    gameId,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(gameId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var participant = CreateApprovedParticipant(Guid.NewGuid(), playerProfile.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    participant.GameId,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.GameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(participant.GameId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenParticipantIsRejected()
        {
            var currentUserId = Guid.NewGuid();
            var playerProfile = CreatePlayerProfile(currentUserId);
            var game = CreateGame();
            var participant = CreateRejectedParticipant(game.Id, playerProfile.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            SetupCurrentUser(
                currentUserServiceMock,
                playerProfileRepositoryMock,
                currentUserId,
                playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    participant.GameId,
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(participant.GameId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static void SetupCurrentUser(
            Mock<ICurrentUserService> currentUserServiceMock,
            Mock<IPlayerProfileRepository> playerProfileRepositoryMock,
            Guid currentUserId,
            PlayerProfile? playerProfile)
        {
            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);
        }

        private static Game CreateGame()
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: 12,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.Open,
                description: "Evening volleyball game");
        }

        private static GameParticipant CreateApprovedParticipant(
            Guid gameId,
            Guid playerProfileId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateRejectedParticipant(
            Guid gameId,
            Guid playerProfileId)
        {
            var participant = GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            participant.Reject();

            return participant;
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