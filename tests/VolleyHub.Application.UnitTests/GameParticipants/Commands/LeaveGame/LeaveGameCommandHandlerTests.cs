using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.LeaveGame;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.LeaveGame
{
    public sealed class LeaveGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCancelParticipantAndSaveChanges_WhenParticipantExists()
        {
            var game = CreateGame();
            var participant = CreateApprovedParticipant(game.Id);
            var playerProfileId = participant.PlayerProfileId;

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfileId,
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
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(
                GameId: game.Id,
                PlayerProfileId: playerProfileId);

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
            var game = CreateGame();
            game.MarkAsFull();

            var participant = CreateApprovedParticipant(game.Id);
            var playerProfileId = participant.PlayerProfileId;

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    game.Id,
                    playerProfileId,
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
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(
                GameId: game.Id,
                PlayerProfileId: playerProfileId);

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
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantDoesNotExist()
        {
            var gameId = Guid.NewGuid();
            var playerProfileId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    gameId,
                    playerProfileId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new LeaveGameCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(
                GameId: gameId,
                PlayerProfileId: playerProfileId);

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
            var participant = CreateApprovedParticipant(Guid.NewGuid());

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    participant.GameId,
                    participant.PlayerProfileId,
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
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(
                GameId: participant.GameId,
                PlayerProfileId: participant.PlayerProfileId);

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
        public async Task Handle_ShouldThrowInvalidOperationException_WhenParticipantIsRejected()
        {
            var game = CreateGame();
            var participant = CreateRejectedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameAndPlayerProfileIdAsync(
                    participant.GameId,
                    participant.PlayerProfileId,
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
                unitOfWorkMock.Object);

            var command = new LeaveGameCommand(
                GameId: participant.GameId,
                PlayerProfileId: participant.PlayerProfileId);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
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

        private static GameParticipant CreateApprovedParticipant(Guid gameId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateRejectedParticipant(Guid gameId)
        {
            var participant = GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            participant.Reject();

            return participant;
        }
    }
}