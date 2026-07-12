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

            var startsAt = DateTimeOffset.UtcNow.AddDays(2);
            var endsAt = startsAt.AddHours(2);

            var command = new UpdateGameCommand(
                Id: game.Id,
                OrganizerId: Guid.NewGuid(),
                CourtId: Guid.NewGuid(),
                StartsAt: startsAt,
                EndsAt: endsAt,
                MaxPlayers: 16,
                PricePerPlayer: 20,
                RequiredLevel: GameLevel.Advanced,
                JoinPolicy: GameJoinPolicy.InviteOnly,
                Description: "Updated game");

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            updatedGame.Should().NotBeNull();
            updatedGame!.Id.Should().Be(game.Id);
            updatedGame.OrganizerId.Should().Be(command.OrganizerId);
            updatedGame.CourtId.Should().Be(command.CourtId);
            updatedGame.StartsAt.Should().Be(command.StartsAt);
            updatedGame.EndsAt.Should().Be(command.EndsAt);
            updatedGame.MaxPlayers.Should().Be(command.MaxPlayers);
            updatedGame.PricePerPlayer.Should().Be(command.PricePerPlayer);
            updatedGame.RequiredLevel.Should().Be(command.RequiredLevel);
            updatedGame.JoinPolicy.Should().Be(command.JoinPolicy);
            updatedGame.Description.Should().Be(command.Description);
            updatedGame.Status.Should().Be(GameStatus.Open);

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

            var startsAt = DateTimeOffset.UtcNow.AddDays(2);

            var command = new UpdateGameCommand(
                Id: gameId,
                OrganizerId: Guid.NewGuid(),
                CourtId: Guid.NewGuid(),
                StartsAt: startsAt,
                EndsAt: startsAt.AddHours(2),
                MaxPlayers: 16,
                PricePerPlayer: 20,
                RequiredLevel: GameLevel.Advanced,
                JoinPolicy: GameJoinPolicy.InviteOnly,
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