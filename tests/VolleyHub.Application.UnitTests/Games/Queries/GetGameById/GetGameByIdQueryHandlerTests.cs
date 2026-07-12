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
            result.OrganizerId.Should().Be(game.OrganizerId);
            result.CourtId.Should().Be(game.CourtId);
            result.StartsAt.Should().Be(game.StartsAt);
            result.EndsAt.Should().Be(game.EndsAt);
            result.MaxPlayers.Should().Be(game.MaxPlayers);
            result.PricePerPlayer.Should().Be(game.PricePerPlayer);
            result.RequiredLevel.Should().Be(game.RequiredLevel);
            result.JoinPolicy.Should().Be(game.JoinPolicy);
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
    }
}