using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Queries.GetGameById;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGameById
{
    public sealed class GetGameByIdQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnGameDto_WhenGameExists()
        {
            // Arrange
            var game = CreateGame();

            var gameRepositoryMock = new Mock<IGameRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            var handler = new GetGameByIdQueryHandler(gameRepositoryMock.Object);

            var query = new GetGameByIdQuery(game.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Id.Should().Be(game.Id);
            result.CourtId.Should().Be(game.CourtId);
            result.StartsAt.Should().Be(game.StartsAt);
            result.MaxPlayers.Should().Be(game.MaxPlayers);
            result.Description.Should().Be(game.Description);
            result.Status.Should().Be(game.Status);

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            // Arrange
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new GetGameByIdQueryHandler(gameRepositoryMock.Object);

            var query = new GetGameByIdQuery(gameId);

            // Act
            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
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