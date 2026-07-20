using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Queries.GetGameById;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGameById
{
    public sealed class GetGameByIdQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnGameDto_WhenGameExists()
        {
            var game = CreateGame();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            var handler = new GetGameByIdQueryHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var query = new GetGameByIdQuery(game.Id);

            var result = await handler.Handle(query, CancellationToken.None);

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
            result.ApprovedParticipantCount.Should().Be(0);
            result.PendingParticipantCount.Should().Be(0);
            result.AvailableSpots.Should().Be(game.MaxPlayers);
            result.CurrentUserJoinStatus.Should().BeNull();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnParticipantSummaryAndCurrentUserJoinStatus_WhenParticipantsExist()
        {
            var currentUserId = Guid.NewGuid();

            var currentPlayerProfile = PlayerProfile.Create(
                userId: currentUserId,
                displayName: "Current Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "Current player profile.");

            var game = CreateGame();

            var approvedParticipant = GameParticipant.JoinOpenGame(
                gameId: game.Id,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var pendingCurrentUserParticipant = GameParticipant.RequestToJoin(
                gameId: game.Id,
                playerProfileId: currentPlayerProfile.Id,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var rejectedParticipant = GameParticipant.RequestToJoin(
                gameId: game.Id,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            rejectedParticipant.Reject();

            var participants = new List<GameParticipant>
    {
        approvedParticipant,
        pendingCurrentUserParticipant,
        rejectedParticipant
    };

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(game);

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentPlayerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            var handler = new GetGameByIdQueryHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(new GetGameByIdQuery(game.Id), CancellationToken.None);

            result.ApprovedParticipantCount.Should().Be(1);
            result.PendingParticipantCount.Should().Be(1);
            result.AvailableSpots.Should().Be(11);
            result.CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Game?)null);

            var handler = new GetGameByIdQueryHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var query = new GetGameByIdQuery(gameId);

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    It.IsAny<Guid>(),
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