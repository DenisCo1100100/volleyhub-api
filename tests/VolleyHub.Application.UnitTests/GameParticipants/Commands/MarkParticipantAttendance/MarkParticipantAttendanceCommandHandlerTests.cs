using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.MarkParticipantAttendance;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.MarkParticipantAttendance
{
    public sealed class MarkParticipantAttendanceCommandHandlerTests
    {
        [Theory]
        [InlineData(GameParticipantAttendanceStatus.Present)]
        [InlineData(GameParticipantAttendanceStatus.Absent)]
        public async Task Handle_ShouldMarkAttendanceAndSaveChanges_WhenGameIsCompletedAndParticipantIsApproved(
            GameParticipantAttendanceStatus attendanceStatus)
        {
            var game = CreateCompletedGame();
            var participant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
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

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new MarkParticipantAttendanceCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new MarkParticipantAttendanceCommand(
                participant.Id,
                attendanceStatus);

            await handler.Handle(command, CancellationToken.None);

            participant.AttendanceStatus.Should().Be(attendanceStatus);

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
            var participantId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participantId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new MarkParticipantAttendanceCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new MarkParticipantAttendanceCommand(
                participantId,
                GameParticipantAttendanceStatus.Present);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
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
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var participant = CreateApprovedParticipant(Guid.NewGuid());

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
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

            var handler = new MarkParticipantAttendanceCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new MarkParticipantAttendanceCommand(
                participant.Id,
                GameParticipantAttendanceStatus.Present);

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
        public async Task Handle_ShouldThrowInvalidOperationException_WhenGameIsNotCompleted()
        {
            var game = CreateOpenGame();
            var participant = CreateApprovedParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
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

            var handler = new MarkParticipantAttendanceCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new MarkParticipantAttendanceCommand(
                participant.Id,
                GameParticipantAttendanceStatus.Present);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenParticipantIsNotApproved()
        {
            var game = CreateCompletedGame();
            var participant = CreatePendingParticipant(game.Id);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
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

            var handler = new MarkParticipantAttendanceCommandHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new MarkParticipantAttendanceCommand(
                participant.Id,
                GameParticipantAttendanceStatus.Present);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Game CreateOpenGame()
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
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Evening volleyball game");
        }

        private static Game CreateCompletedGame()
        {
            var game = CreateOpenGame();

            game.Complete();

            return game;
        }

        private static GameParticipant CreateApprovedParticipant(Guid gameId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }

        private static GameParticipant CreatePendingParticipant(Guid gameId)
        {
            return GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }
    }
}