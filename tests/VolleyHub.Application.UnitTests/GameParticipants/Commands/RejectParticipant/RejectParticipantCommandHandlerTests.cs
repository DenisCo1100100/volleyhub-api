using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Commands.RejectParticipant;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.RejectParticipant
{
    public sealed class RejectParticipantCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldRejectParticipantAndSaveChanges_WhenParticipantExists()
        {
            var participant = CreatePendingParticipant();

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new RejectParticipantCommandHandler(
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new RejectParticipantCommand(participant.Id);

            await handler.Handle(command, CancellationToken.None);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Rejected);

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

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participantId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((GameParticipant?)null);

            var handler = new RejectParticipantCommandHandler(
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new RejectParticipantCommand(participantId);

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
        public async Task Handle_ShouldThrowInvalidOperationException_WhenParticipantIsAlreadyApproved()
        {
            var participant = GameParticipant.JoinOpenGame(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    participant.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            var handler = new RejectParticipantCommandHandler(
                gameParticipantRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new RejectParticipantCommand(participant.Id);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<GameParticipant>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static GameParticipant CreatePendingParticipant()
        {
            return GameParticipant.RequestToJoin(
                gameId: Guid.NewGuid(),
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }
    }
}