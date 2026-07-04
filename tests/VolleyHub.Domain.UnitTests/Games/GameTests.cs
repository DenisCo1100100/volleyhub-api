using FluentAssertions;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameTests
    {
        [Fact]
        public void Create_ShouldCreateGame_WhenDataIsValid()
        {
            // Arrange
            var courtId = Guid.NewGuid();
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            // Act
            var game = Game.Create(
                courtId: courtId,
                startsAt: startsAt,
                maxPlayers: 12,
                description: "Evening volleyball game");

            // Assert
            game.Id.Should().NotBeEmpty();
            game.CourtId.Should().Be(courtId);
            game.StartsAt.Should().Be(startsAt);
            game.MaxPlayers.Should().Be(12);
            game.Description.Should().Be("Evening volleyball game");
            game.Status.Should().Be(GameStatus.Scheduled);
        }

        [Fact]
        public void Create_ShouldTrimDescription_WhenDescriptionContainsWhitespaces()
        {
            // Act
            var game = Game.Create(
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                description: "  Evening volleyball game  ");

            // Assert
            game.Description.Should().Be("Evening volleyball game");
        }

        [Fact]
        public void Create_ShouldSetDescriptionToNull_WhenDescriptionIsWhiteSpace()
        {
            // Act
            var game = Game.Create(
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                description: "   ");

            // Assert
            game.Description.Should().BeNull();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenCourtIdIsEmpty()
        {
            // Act
            Action act = () => Game.Create(
                courtId: Guid.Empty,
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenStartsAtIsDefault()
        {
            // Act
            Action act = () => Game.Create(
                courtId: Guid.NewGuid(),
                startsAt: default,
                maxPlayers: 12,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(25)]
        public void Create_ShouldThrowArgumentException_WhenMaxPlayersIsOutOfRange(int maxPlayers)
        {
            // Act
            Action act = () => Game.Create(
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: maxPlayers,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenDescriptionIsTooLong()
        {
            // Arrange
            var description = new string('a', Game.MaxDescriptionLength + 1);

            // Act
            Action act = () => Game.Create(
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                description: description);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void UpdateDetails_ShouldUpdateGame_WhenGameIsScheduled()
        {
            // Arrange
            var game = CreateGame();
            var newCourtId = Guid.NewGuid();
            var newStartsAt = DateTimeOffset.UtcNow.AddDays(2);

            // Act
            game.UpdateDetails(
                courtId: newCourtId,
                startsAt: newStartsAt,
                maxPlayers: 16,
                description: "Updated game");

            // Assert
            game.CourtId.Should().Be(newCourtId);
            game.StartsAt.Should().Be(newStartsAt);
            game.MaxPlayers.Should().Be(16);
            game.Description.Should().Be("Updated game");
            game.Status.Should().Be(GameStatus.Scheduled);
        }

        [Fact]
        public void Cancel_ShouldSetStatusToCancelled_WhenGameIsScheduled()
        {
            // Arrange
            var game = CreateGame();

            // Act
            game.Cancel();

            // Assert
            game.Status.Should().Be(GameStatus.Cancelled);
        }

        [Fact]
        public void Complete_ShouldSetStatusToCompleted_WhenGameIsScheduled()
        {
            // Arrange
            var game = CreateGame();

            // Act
            game.Complete();

            // Assert
            game.Status.Should().Be(GameStatus.Completed);
        }

        [Fact]
        public void UpdateDetails_ShouldThrowInvalidOperationException_WhenGameIsCancelled()
        {
            // Arrange
            var game = CreateGame();
            game.Cancel();

            // Act
            Action act = () => game.UpdateDetails(
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(2),
                maxPlayers: 16,
                description: "Updated game");

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Complete_ShouldThrowInvalidOperationException_WhenGameIsCancelled()
        {
            // Arrange
            var game = CreateGame();
            game.Cancel();

            // Act
            Action act = () => game.Complete();

            // Assert
            act.Should().Throw<InvalidOperationException>();
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