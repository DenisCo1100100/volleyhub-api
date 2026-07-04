using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Commands.CreateGame;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.Games.Commands.CreateGame
{
    public sealed class CreateGameCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldAddGameAndSaveChanges_WhenCommandIsValid()
        {
            // Arrange
            var gameRepositoryMock = new Mock<IGameRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            Game? addedGame = null;

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
                unitOfWorkMock.Object);

            var command = new CreateGameCommand(
                CourtId: Guid.NewGuid(),
                StartsAt: DateTimeOffset.UtcNow.AddDays(1),
                MaxPlayers: 12,
                Description: "Evening volleyball game");

            // Act
            var gameId = await handler.Handle(command, CancellationToken.None);

            // Assert
            gameId.Should().NotBeEmpty();

            addedGame.Should().NotBeNull();
            addedGame!.Id.Should().Be(gameId);
            addedGame.CourtId.Should().Be(command.CourtId);
            addedGame.StartsAt.Should().Be(command.StartsAt);
            addedGame.MaxPlayers.Should().Be(command.MaxPlayers);
            addedGame.Description.Should().Be(command.Description);
            addedGame.Status.Should().Be(GameStatus.Scheduled);

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
    }
}