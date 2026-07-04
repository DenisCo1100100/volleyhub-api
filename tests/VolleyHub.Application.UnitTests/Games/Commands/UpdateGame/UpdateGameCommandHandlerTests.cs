using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.Games.Commands.UpdateGame
{
    public sealed class UpdateGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldUpdateGameAndSaveChanges_WhenGameExists()
        {
            // Arrange
            var game = CreateGame();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            Game? updatedGame = null;

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
                unitOfWorkMock.Object);

            var command = new UpdateGameCommand(
                Id: game.Id,
                CourtId: Guid.NewGuid(),
                StartsAt: DateTimeOffset.UtcNow.AddDays(2),
                MaxPlayers: 16,
                Description: "Updated game");

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            updatedGame.Should().NotBeNull();
            updatedGame!.Id.Should().Be(game.Id);
            updatedGame.CourtId.Should().Be(command.CourtId);
            updatedGame.StartsAt.Should().Be(command.StartsAt);
            updatedGame.MaxPlayers.Should().Be(command.MaxPlayers);
            updatedGame.Description.Should().Be(command.Description);
            updatedGame.Status.Should().Be(GameStatus.Scheduled);

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            // Arrange
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new UpdateGameCommandHandler(
                gameRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new UpdateGameCommand(
                Id: gameId,
                CourtId: Guid.NewGuid(),
                StartsAt: DateTimeOffset.UtcNow.AddDays(2),
                MaxPlayers: 16,
                Description: "Updated game");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Game>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Game CreateGame()
        {
            return Game.Create(
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                description: "Evening volleyball game");
        }
    }
}