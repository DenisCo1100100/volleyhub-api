using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Queries.GetGames;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnGameDtos_WhenGamesExist()
        {
            // Arrange
            var games = new List<Game>
            {
                Game.Create(
                    courtId: Guid.NewGuid(),
                    startsAt: DateTimeOffset.UtcNow.AddDays(1),
                    maxPlayers: 12,
                    description: "Evening volleyball game"),

                Game.Create(
                    courtId: Guid.NewGuid(),
                    startsAt: DateTimeOffset.UtcNow.AddDays(2),
                    maxPlayers: 16,
                    description: "Weekend volleyball game")
            };

            var gameRepositoryMock = new Mock<IGameRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(games);

            var handler = new GetGamesQueryHandler(gameRepositoryMock.Object);

            var query = new GetGamesQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);

            result[0].Id.Should().Be(games[0].Id);
            result[0].CourtId.Should().Be(games[0].CourtId);
            result[0].StartsAt.Should().Be(games[0].StartsAt);
            result[0].MaxPlayers.Should().Be(games[0].MaxPlayers);
            result[0].Description.Should().Be(games[0].Description);
            result[0].Status.Should().Be(games[0].Status);

            result[1].Id.Should().Be(games[1].Id);
            result[1].CourtId.Should().Be(games[1].CourtId);
            result[1].StartsAt.Should().Be(games[1].StartsAt);
            result[1].MaxPlayers.Should().Be(games[1].MaxPlayers);
            result[1].Description.Should().Be(games[1].Description);
            result[1].Status.Should().Be(games[1].Status);

            gameRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenGamesDoNotExist()
        {
            // Arrange
            var gameRepositoryMock = new Mock<IGameRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Game>());

            var handler = new GetGamesQueryHandler(gameRepositoryMock.Object);

            var query = new GetGamesQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();

            gameRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}