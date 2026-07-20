using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Queries.GetGames;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnGameDtos_WhenGamesExist()
        {
            var games = new List<Game>
            {
                CreateGame(
                    startsAt: DateTimeOffset.UtcNow.AddDays(1),
                    maxPlayers: 12,
                    description: "Evening volleyball game"),

                CreateGame(
                    startsAt: DateTimeOffset.UtcNow.AddDays(2),
                    maxPlayers: 16,
                    description: "Weekend volleyball game")
            };

            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(games);

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var query = new GetGamesQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(2);

            result[0].Id.Should().Be(games[0].Id);
            result[0].OrganizerId.Should().Be(games[0].OrganizerId);
            result[0].CourtId.Should().Be(games[0].CourtId);
            result[0].StartsAt.Should().Be(games[0].StartsAt);
            result[0].EndsAt.Should().Be(games[0].EndsAt);
            result[0].MaxPlayers.Should().Be(games[0].MaxPlayers);
            result[0].PricePerPlayer.Should().Be(games[0].PricePerPlayer);
            result[0].RequiredLevel.Should().Be(games[0].RequiredLevel);
            result[0].JoinPolicy.Should().Be(games[0].JoinPolicy);
            result[0].Description.Should().Be(games[0].Description);
            result[0].Status.Should().Be(games[0].Status);
            result[0].ApprovedParticipantCount.Should().Be(0);
            result[0].PendingParticipantCount.Should().Be(0);
            result[0].AvailableSpots.Should().Be(games[0].MaxPlayers);
            result[0].CurrentUserJoinStatus.Should().BeNull();

            result[1].Id.Should().Be(games[1].Id);
            result[1].OrganizerId.Should().Be(games[1].OrganizerId);
            result[1].CourtId.Should().Be(games[1].CourtId);
            result[1].StartsAt.Should().Be(games[1].StartsAt);
            result[1].EndsAt.Should().Be(games[1].EndsAt);
            result[1].MaxPlayers.Should().Be(games[1].MaxPlayers);
            result[1].PricePerPlayer.Should().Be(games[1].PricePerPlayer);
            result[1].RequiredLevel.Should().Be(games[1].RequiredLevel);
            result[1].JoinPolicy.Should().Be(games[1].JoinPolicy);
            result[1].Description.Should().Be(games[1].Description);
            result[1].Status.Should().Be(games[1].Status);
            result[1].ApprovedParticipantCount.Should().Be(0);
            result[1].PendingParticipantCount.Should().Be(0);
            result[1].AvailableSpots.Should().Be(games[1].MaxPlayers);
            result[1].CurrentUserJoinStatus.Should().BeNull();

            gameRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

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

            var game = CreateGame(
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                description: "Evening volleyball game");

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
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Game> { game });

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

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(new GetGamesQuery(), CancellationToken.None);

            result.Should().HaveCount(1);

            result[0].ApprovedParticipantCount.Should().Be(1);
            result[0].PendingParticipantCount.Should().Be(1);
            result[0].AvailableSpots.Should().Be(11);
            result[0].CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);

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
        public async Task Handle_ShouldReturnEmptyList_WhenGamesDoNotExist()
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Game>());

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var query = new GetGamesQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().BeEmpty();

            gameRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Game CreateGame(
            DateTimeOffset startsAt,
            int maxPlayers,
            string description)
        {
            return Game.Create(
                organizerId: Guid.NewGuid(),
                courtId: Guid.NewGuid(),
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: maxPlayers,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: description);
        }
    }
}