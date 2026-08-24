using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Queries.GetGameById;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGameById
{
    public sealed class GetGameByIdQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnFrontendGameDetails_WhenGameExists()
        {
            var gameContext = CreateGameContext();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var courtRepositoryMock = new Mock<ICourtRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(gameContext.Game);

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

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            var handler = new GetGameByIdQueryHandler(
                gameRepositoryMock.Object,
                courtRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGameByIdQuery(gameContext.Game.Id),
                CancellationToken.None);

            result.Id.Should().Be(gameContext.Game.Id);

            result.Court.Id.Should().Be(gameContext.Court.Id);
            result.Court.Name.Should().Be(gameContext.Court.Name);
            result.Court.Address.Should().Be(gameContext.Court.Address);
            result.Court.Latitude.Should().Be(gameContext.Court.Latitude);
            result.Court.Longitude.Should().Be(gameContext.Court.Longitude);
            result.Court.SurfaceType.Should().Be(gameContext.Court.SurfaceType);
            result.Court.IsIndoor.Should().Be(gameContext.Court.IsIndoor);

            result.Organizer.Id.Should().Be(gameContext.Organizer.Id);
            result.Organizer.DisplayName.Should().Be(gameContext.Organizer.DisplayName);
            result.Organizer.SkillLevel.Should().Be(gameContext.Organizer.SkillLevel);

            result.StartsAt.Should().Be(gameContext.Game.StartsAt);
            result.EndsAt.Should().Be(gameContext.Game.EndsAt);
            result.MaxPlayers.Should().Be(gameContext.Game.MaxPlayers);
            result.PricePerPlayer.Should().Be(gameContext.Game.PricePerPlayer);
            result.RequiredLevel.Should().Be(gameContext.Game.RequiredLevel);
            result.JoinPolicy.Should().Be(gameContext.Game.JoinPolicy);
            result.Description.Should().Be(gameContext.Game.Description);
            result.Status.Should().Be(gameContext.Game.Status);
            result.ApprovedParticipantCount.Should().Be(0);
            result.PendingParticipantCount.Should().Be(0);
            result.AvailableSpots.Should().Be(gameContext.Game.MaxPlayers);
            result.CurrentUserJoinStatus.Should().BeNull();
            result.Participants.Should().BeEmpty();
            result.CreatedAt.Should().Be(gameContext.Game.CreatedAt);
            result.UpdatedAt.Should().Be(gameContext.Game.UpdatedAt);

            gameRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    gameContext.Court.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnParticipantDetailsAndCurrentUserState_WhenParticipantsExist()
        {
            var gameContext = CreateGameContext();

            var approvedPlayer = CreatePlayerProfile(
                Guid.NewGuid(),
                "Approved Player");

            var currentUserId = Guid.NewGuid();

            var currentPlayer = CreatePlayerProfile(
                currentUserId,
                "Current Player");

            var approvedParticipant = GameParticipant.JoinOpenGame(
                gameId: gameContext.Game.Id,
                playerProfileId: approvedPlayer.Id,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var pendingParticipant = GameParticipant.RequestToJoin(
                gameId: gameContext.Game.Id,
                playerProfileId: currentPlayer.Id,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var participants = new List<GameParticipant>
            {
                approvedParticipant,
                pendingParticipant
            };

            var gameRepositoryMock = new Mock<IGameRepository>();
            var courtRepositoryMock = new Mock<ICourtRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(gameContext.Game);

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
                .Setup(repository => repository.GetByIdAsync(
                    approvedPlayer.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(approvedPlayer);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    currentPlayer.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentPlayer);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentPlayer);

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameContext.Game.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            var handler = new GetGameByIdQueryHandler(
                gameRepositoryMock.Object,
                courtRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGameByIdQuery(gameContext.Game.Id),
                CancellationToken.None);

            result.ApprovedParticipantCount.Should().Be(1);
            result.PendingParticipantCount.Should().Be(1);
            result.AvailableSpots.Should().Be(11);
            result.CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);

            result.Participants.Should().HaveCount(2);

            var approvedResult = result.Participants.Single(
                participant => participant.Id == approvedParticipant.Id);

            approvedResult.PlayerProfileId.Should().Be(approvedPlayer.Id);
            approvedResult.DisplayName.Should().Be(approvedPlayer.DisplayName);
            approvedResult.SkillLevel.Should().Be(approvedPlayer.SkillLevel);
            approvedResult.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            approvedResult.AttendanceStatus.Should().Be(approvedParticipant.AttendanceStatus);
            approvedResult.OfflinePaymentStatus.Should().Be(approvedParticipant.OfflinePaymentStatus);
            approvedResult.JoinedAt.Should().Be(approvedParticipant.JoinedAt);
            approvedResult.ApprovedAt.Should().Be(approvedParticipant.ApprovedAt);

            var pendingResult = result.Participants.Single(
                participant => participant.Id == pendingParticipant.Id);

            pendingResult.PlayerProfileId.Should().Be(currentPlayer.Id);
            pendingResult.DisplayName.Should().Be(currentPlayer.DisplayName);
            pendingResult.SkillLevel.Should().Be(currentPlayer.SkillLevel);
            pendingResult.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenGameDoesNotExist()
        {
            var gameId = Guid.NewGuid();

            var gameRepositoryMock = new Mock<IGameRepository>();
            var courtRepositoryMock = new Mock<ICourtRepository>();
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
                courtRepositoryMock.Object,
                gameParticipantRepositoryMock.Object,
                currentUserServiceMock.Object,
                playerProfileRepositoryMock.Object);

            Func<Task> act = async () =>
                await handler.Handle(
                    new GetGameByIdQuery(gameId),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

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

        private static GameContext CreateGameContext()
        {
            var organizer = CreatePlayerProfile(
                Guid.NewGuid(),
                "Game Organizer");

            var court = Court.Create(
                ownerPlayerProfileId: organizer.Id,
                name: "Central Beach Court",
                address: "Test Street 10",
                latitude: 52.3676,
                longitude: 4.9041,
                surfaceType: CourtSurfaceType.Indoor,
                isIndoor: true,
                description: "Test court.");

            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            var game = Game.Create(
                organizerId: organizer.Id,
                courtId: court.Id,
                startsAt: startsAt,
                endsAt: startsAt.AddHours(2),
                maxPlayers: 12,
                pricePerPlayer: 15,
                requiredLevel: GameLevel.Intermediate,
                joinPolicy: GameJoinPolicy.ApprovalRequired,
                description: "Evening volleyball game");

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