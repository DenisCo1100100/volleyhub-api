using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.ApproveParticipant;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.ApproveParticipant
{
    public sealed class ApproveParticipantCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldApproveParticipantAndSaveChanges_WhenParticipantExists()
        {
            var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var approvedAt = DateTimeOffset.UtcNow;

            var game = CreateGame();
            var participant = CreatePendingParticipant(game.Id, joinedAt);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant> { participant });

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(approvedAt);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.ApprovedAt.Should().Be(approvedAt);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(participant),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMarkGameAsFull_WhenApprovedParticipantReachesMaxPlayers()
        {
            var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var approvedAt = DateTimeOffset.UtcNow;

            var game = CreateGame(maxPlayers: 2);
            var pendingParticipant = CreatePendingParticipant(game.Id, joinedAt);
            var approvedParticipant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    pendingParticipant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingParticipant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>
                {
                    pendingParticipant,
                    approvedParticipant
                });

            dateTimeProviderMock
                .Setup(provider => provider.UtcNow)
                .Returns(approvedAt);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(pendingParticipant.Id);

            await handler.Handle(command, CancellationToken.None);

            pendingParticipant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            game.Status.Should().Be(GameStatus.Full);

            gameRepositoryMock.Verify(
                repository => repository.Update(game),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(pendingParticipant),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantDoesNotExist()
        {
            var participantId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participantId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participantId);

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
            var participant = CreatePendingParticipant(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-10));

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.GameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

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
        public async Task Handle_ShouldThrowInvalidOperationException_WhenGameIsFull()
        {
            var joinedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var game = CreateGame(maxPlayers: 2);
            game.MarkAsFull();

            var participant = CreatePendingParticipant(game.Id, joinedAt);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var dateTimeProviderMock = new Mock<IDateTimeProvider>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            var handler = new ApproveParticipantCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                dateTimeProviderMock.Object,
                unitOfWorkMock.Object);

            var command = new ApproveParticipantCommand(participant.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Game CreateGame(int maxPlayers = 12)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: maxPlayers,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Evening volleyball game");
        }

        private static GameParticipant CreatePendingParticipant(
            Guid gameId,
            DateTimeOffset joinedAt)
        {
            return GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: joinedAt,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreateApprovedParticipant(Guid gameId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }
    }
}