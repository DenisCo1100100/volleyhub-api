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
            var organizerId = Guid.NewGuid();
            var courtId = Guid.NewGuid();
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var endsAt = startsAt.AddHours(2);

            // Act
            var game = Game.Create(
                organizerId: organizerId,
                courtId: courtId,
                startsAt: startsAt,
                endsAt: endsAt,
                maxPlayers: 12,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Evening volleyball game");

            // Assert
            game.Id.Should().NotBeEmpty();
            game.OrganizerId.Should().Be(organizerId);
            game.CourtId.Should().Be(courtId);
            game.StartsAt.Should().Be(startsAt);
            game.EndsAt.Should().Be(endsAt);
            game.MaxPlayers.Should().Be(12);
            game.PricePerPlayer.Should().Be(15);
            game.RequiredLevel.Should().Be(GameLevel.Intermediate);
            game.JoinPolicy.Should().Be(GameJoinPolicy.ApprovalRequired);
            game.Description.Should().Be("Evening volleyball game");
            game.Status.Should().Be(GameStatus.Open);
        }

        [Fact]
        public void Create_ShouldCreateGame_WhenEndsAtIsNull()
        {
            // Act
            var game = Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            game.EndsAt.Should().BeNull();
            game.PricePerPlayer.Should().Be(0);
            game.RequiredLevel.Should().Be(GameLevel.Any);
            game.JoinPolicy.Should().Be(GameJoinPolicy.Open);
            game.Status.Should().Be(GameStatus.Open);
        }

        [Fact]
        public void Create_ShouldTrimDescription_WhenDescriptionContainsWhitespaces()
        {
            // Act
            var game = Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: "  Evening volleyball game  ");

            // Assert
            game.Description.Should().Be("Evening volleyball game");
        }

        [Fact]
        public void Create_ShouldSetDescriptionToNull_WhenDescriptionIsWhiteSpace()
        {
            // Act
            var game = Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: "   ");

            // Assert
            game.Description.Should().BeNull();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenOrganizerIdIsEmpty()
        {
            // Act
            Action act = () => Game.Create(
                organizerId: Guid.Empty,
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenCourtIdIsEmpty()
        {
            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.Empty,
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenStartsAtIsDefault()
        {
            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: default,
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenEndsAtIsBeforeStartsAt()
        {
            // Arrange
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var endsAt = startsAt.AddMinutes(-1);

            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: endsAt,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenEndsAtEqualsStartsAt()
        {
            // Arrange
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
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
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: maxPlayers,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenPricePerPlayerIsNegative()
        {
            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: -1,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(GameLevel.Unknown)]
        [InlineData((GameLevel)999)]
        public void Create_ShouldThrowArgumentException_WhenRequiredLevelIsInvalid(GameLevel requiredLevel)
        {
            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: requiredLevel,
                joinPolicy: GameJoinPolicy.Open,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(GameJoinPolicy.Unknown)]
        [InlineData((GameJoinPolicy)999)]
        public void Create_ShouldThrowArgumentException_WhenJoinPolicyIsInvalid(GameJoinPolicy joinPolicy)
        {
            // Act
            Action act = () => Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: joinPolicy,
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
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                endsAt: null,
                maxPlayers: 12,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
                description: description);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void UpdateDetails_ShouldUpdateGame_WhenGameIsOpen()
        {
            // Arrange
            var game = CreateGame();
            var newOrganizerId = Guid.NewGuid();
            var newCourtId = Guid.NewGuid();
            var newStartsAt = DateTimeOffset.UtcNow.AddDays(2);
            var newEndsAt = newStartsAt.AddHours(2);

            // Act
            game.UpdateDetails(
                organizerId: newOrganizerId,
                courtId: newCourtId,
                startsAt: newStartsAt,
                endsAt: newEndsAt,
                maxPlayers: 16,
                pricePerPlayer: 20,
                requiredLevel: GameLevel.Advanced,
                joinPolicy: GameJoinPolicy.InviteOnly,
                description: "Updated game");

            // Assert
            game.OrganizerId.Should().Be(newOrganizerId);
            game.CourtId.Should().Be(newCourtId);
            game.StartsAt.Should().Be(newStartsAt);
            game.EndsAt.Should().Be(newEndsAt);
            game.MaxPlayers.Should().Be(16);
            game.PricePerPlayer.Should().Be(20);
            game.RequiredLevel.Should().Be(GameLevel.Advanced);
            game.JoinPolicy.Should().Be(GameJoinPolicy.InviteOnly);
            game.Description.Should().Be("Updated game");
            game.Status.Should().Be(GameStatus.Open);
        }

        [Fact]
        public void MarkAsFull_ShouldSetStatusToFull_WhenGameIsOpen()
        {
            // Arrange
            var game = CreateGame();

            // Act
            game.MarkAsFull();

            // Assert
            game.Status.Should().Be(GameStatus.Full);
        }

        [Fact]
        public void Reopen_ShouldSetStatusToOpen_WhenGameIsFull()
        {
            // Arrange
            var game = CreateGame();
            game.MarkAsFull();

            // Act
            game.Reopen();

            // Assert
            game.Status.Should().Be(GameStatus.Open);
        }

        [Fact]
        public void Cancel_ShouldSetStatusToCancelled_WhenGameIsOpen()
        {
            // Arrange
            var game = CreateGame();

            // Act
            game.Cancel();

            // Assert
            game.Status.Should().Be(GameStatus.Cancelled);
        }

        [Fact]
        public void Complete_ShouldSetStatusToCompleted_WhenGameIsOpen()
        {
            // Arrange
            var game = CreateGame();

            // Act
            game.Complete();

            // Assert
            game.Status.Should().Be(GameStatus.Completed);
        }

        [Fact]
        public void Complete_ShouldSetStatusToCompleted_WhenGameIsFull()
        {
            // Arrange
            var game = CreateGame();
            game.MarkAsFull();

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
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: DateTimeOffset.UtcNow.AddDays(2),
                endsAt: null,
                maxPlayers: 16,
                pricePerPlayer: 0,
                requiredLevel: GameLevel.Any,
                joinPolicy: GameJoinPolicy.Open,
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

        [Fact]
        public void Cancel_ShouldThrowInvalidOperationException_WhenGameIsCompleted()
        {
            // Arrange
            var game = CreateGame();
            game.Complete();

            // Act
            Action act = () => game.Cancel();

            // Assert
            act.Should().Throw<InvalidOperationException>();
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