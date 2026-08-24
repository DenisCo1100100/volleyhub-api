using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Queries.GetGames;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnFrontendGameSummaries_WhenGamesExist()
        {
            var firstGame = CreateGameContext(
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                courtName: "Central Beach Court",
                organizerName: "First Organizer");

            var secondGame = CreateGameContext(
                startsAt: DateTimeOffset.UtcNow.AddDays(2),
                maxPlayers: 16,
                courtName: "Indoor Arena",
                organizerName: "Second Organizer");

            var games = new List<Game>
            {
                firstGame.Game,
                secondGame.Game
            };

            var gameRepositoryMock = new Mock<IGameRepository>();
            var courtRepositoryMock = new Mock<ICourtRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(games);

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    firstGame.Court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstGame.Court);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    secondGame.Court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(secondGame.Court);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    firstGame.Organizer.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstGame.Organizer);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    secondGame.Organizer.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(secondGame.Organizer);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                courtRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGamesQuery(),
                CancellationToken.None);

            result.Should().HaveCount(2);

            result[0].Id.Should().Be(firstGame.Game.Id);
            result[0].Court.Id.Should().Be(firstGame.Court.Id);
            result[0].Court.Name.Should().Be(firstGame.Court.Name);
            result[0].Court.Address.Should().Be(firstGame.Court.Address);
            result[0].Court.Latitude.Should().Be(firstGame.Court.Latitude);
            result[0].Court.Longitude.Should().Be(firstGame.Court.Longitude);
            result[0].Court.SurfaceType.Should().Be(firstGame.Court.SurfaceType);
            result[0].Court.IsIndoor.Should().Be(firstGame.Court.IsIndoor);
            result[0].Organizer.Id.Should().Be(firstGame.Organizer.Id);
            result[0].Organizer.DisplayName.Should().Be(firstGame.Organizer.DisplayName);
            result[0].Organizer.SkillLevel.Should().Be(firstGame.Organizer.SkillLevel);
            result[0].StartsAt.Should().Be(firstGame.Game.StartsAt);
            result[0].EndsAt.Should().Be(firstGame.Game.EndsAt);
            result[0].MaxPlayers.Should().Be(firstGame.Game.MaxPlayers);
            result[0].PricePerPlayer.Should().Be(firstGame.Game.PricePerPlayer);
            result[0].RequiredLevel.Should().Be(firstGame.Game.RequiredLevel);
            result[0].JoinPolicy.Should().Be(firstGame.Game.JoinPolicy);
            result[0].Status.Should().Be(firstGame.Game.Status);
            result[0].ApprovedParticipantCount.Should().Be(0);
            result[0].PendingParticipantCount.Should().Be(0);
            result[0].AvailableSpots.Should().Be(firstGame.Game.MaxPlayers);
            result[0].CurrentUserJoinStatus.Should().BeNull();

            result[1].Id.Should().Be(secondGame.Game.Id);
            result[1].Court.Id.Should().Be(secondGame.Court.Id);
            result[1].Court.Name.Should().Be(secondGame.Court.Name);
            result[1].Organizer.Id.Should().Be(secondGame.Organizer.Id);
            result[1].Organizer.DisplayName.Should().Be(secondGame.Organizer.DisplayName);
            result[1].StartsAt.Should().Be(secondGame.Game.StartsAt);
            result[1].MaxPlayers.Should().Be(secondGame.Game.MaxPlayers);
            result[1].ApprovedParticipantCount.Should().Be(0);
            result[1].PendingParticipantCount.Should().Be(0);
            result[1].AvailableSpots.Should().Be(secondGame.Game.MaxPlayers);
            result[1].CurrentUserJoinStatus.Should().BeNull();

            gameRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

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

            var currentPlayerProfile = CreatePlayerProfile(
                currentUserId,
                "Current Player");

            var gameContext = CreateGameContext(
                startsAt: DateTimeOffset.UtcNow.AddDays(1),
                maxPlayers: 12,
                courtName: "Central Court",
                organizerName: "Organizer");

            var approvedParticipant = GameParticipant.JoinOpenGame(
                gameId: gameContext.Game.Id,
                playerProfileId: Guid.NewGuid(),
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var pendingCurrentUserParticipant = GameParticipant.RequestToJoin(
                gameId: gameContext.Game.Id,
                playerProfileId: currentPlayerProfile.Id,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var rejectedParticipant = GameParticipant.RequestToJoin(
                gameId: gameContext.Game.Id,
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
            var courtRepositoryMock = new Mock<ICourtRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Game> { gameContext.Game });

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameContext.Court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(gameContext.Court);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameContext.Organizer.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(gameContext.Organizer);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentPlayerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                courtRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGamesQuery(),
                CancellationToken.None);

            result.Should().HaveCount(1);

            result[0].Court.Name.Should().Be(gameContext.Court.Name);
            result[0].Organizer.DisplayName.Should().Be(gameContext.Organizer.DisplayName);
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
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenGamesDoNotExist()
        {
            var gameRepositoryMock = new Mock<IGameRepository>();
            var courtRepositoryMock = new Mock<ICourtRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Game>());

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                courtRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGamesQuery(),
                CancellationToken.None);

            result.Should().BeEmpty();

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static GameContext CreateGameContext(
            DateTimeOffset startsAt,
            int maxPlayers,
            string courtName,
            string organizerName)
        {
            var organizer = CreatePlayerProfile(
                Guid.NewGuid(),
                organizerName);

            var court = Court.Create(
                ownerPlayerProfileId: organizer.Id,
                name: courtName,
                address: "Test Street 10",
                latitude: 52.3676,
                longitude: 4.9041,
                surfaceType: CourtSurfaceType.Indoor,
                isIndoor: true,
                description: "Test court.");

            var game = Game.Create(
                organizerId: organizer.Id,
                courtId: court.Id,
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: maxPlayers,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Test game.");

            return new GameContext(
                game,
                court,
                organizer);
        }

        private static PlayerProfile CreatePlayerProfile(
            Guid userId,
            string displayName)
        {
            return PlayerProfile.Create(
                userId: userId,
                displayName: displayName,
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "Test player profile.");
        }

        private sealed record GameContext(
            Game Game,
            Court Court,
            PlayerProfile Organizer);
    }
}