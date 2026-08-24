using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Queries.GetGameParticipants;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.GameParticipants.Queries.GetGameParticipants
{
    public sealed class GetGameParticipantsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnParticipantSummaries_WhenParticipantsExist()
        {
            var gameId = Guid.NewGuid();

            var firstPlayerProfile = CreatePlayerProfile(
                "First Player",
                PlayerSkillLevel.Intermediate);

            var secondPlayerProfile = CreatePlayerProfile(
                "Second Player",
                PlayerSkillLevel.Advanced);

            var firstParticipant = GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: firstPlayerProfile.Id,
                joinedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var secondParticipant = GameParticipant.RequestToJoin(
                gameId: gameId,
                playerProfileId: secondPlayerProfile.Id,
                joinedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);

            var participants = new List<GameParticipant>
            {
                firstParticipant,
                secondParticipant
            };

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    firstPlayerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstPlayerProfile);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    secondPlayerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(secondPlayerProfile);

            var handler = new GetGameParticipantsQueryHandler(
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGameParticipantsQuery(gameId),
                CancellationToken.None);

            result.Should().HaveCount(2);

            result[0].Id.Should().Be(firstParticipant.Id);
            result[0].PlayerProfileId.Should().Be(firstPlayerProfile.Id);
            result[0].DisplayName.Should().Be(firstPlayerProfile.DisplayName);
            result[0].SkillLevel.Should().Be(firstPlayerProfile.SkillLevel);
            result[0].JoinedAt.Should().Be(firstParticipant.JoinedAt);
            result[0].ApprovedAt.Should().Be(firstParticipant.ApprovedAt);
            result[0].JoinStatus.Should().Be(firstParticipant.JoinStatus);
            result[0].AttendanceStatus.Should().Be(firstParticipant.AttendanceStatus);
            result[0].OfflinePaymentStatus.Should().Be(firstParticipant.OfflinePaymentStatus);

            result[1].Id.Should().Be(secondParticipant.Id);
            result[1].PlayerProfileId.Should().Be(secondPlayerProfile.Id);
            result[1].DisplayName.Should().Be(secondPlayerProfile.DisplayName);
            result[1].SkillLevel.Should().Be(secondPlayerProfile.SkillLevel);
            result[1].JoinedAt.Should().Be(secondParticipant.JoinedAt);
            result[1].ApprovedAt.Should().Be(secondParticipant.ApprovedAt);
            result[1].JoinStatus.Should().Be(secondParticipant.JoinStatus);
            result[1].AttendanceStatus.Should().Be(secondParticipant.AttendanceStatus);
            result[1].OfflinePaymentStatus.Should().Be(secondParticipant.OfflinePaymentStatus);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenParticipantsDoNotExist()
        {
            var gameId = Guid.NewGuid();

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            var handler = new GetGameParticipantsQueryHandler(
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object);

            var result = await handler.Handle(
                new GetGameParticipantsQuery(gameId),
                CancellationToken.None);

            result.Should().BeEmpty();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantPlayerProfileDoesNotExist()
        {
            var gameId = Guid.NewGuid();
            var playerProfileId = Guid.NewGuid();

            var participant = GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: playerProfileId,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>
                {
                    participant
                });

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfileId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new GetGameParticipantsQueryHandler(
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new GetGameParticipantsQuery(gameId),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenParticipantPlayerProfileIsDeleted()
        {
            var gameId = Guid.NewGuid();

            var playerProfile = CreatePlayerProfile(
                "Deleted Player",
                PlayerSkillLevel.Beginner);

            playerProfile.Delete();

            var participant = GameParticipant.JoinOpenGame(
                gameId: gameId,
                playerProfileId: playerProfile.Id,
                joinedAt: DateTimeOffset.UtcNow,
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired);

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>
                {
                    participant
                });

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new GetGameParticipantsQueryHandler(
                gameParticipantRepositoryMock.Object,
                playerProfileRepositoryMock.Object);

            Func<Task> act = async () => await handler.Handle(
                new GetGameParticipantsQuery(gameId),
                CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        private static PlayerProfile CreatePlayerProfile(
            string displayName,
            PlayerSkillLevel skillLevel)
        {
            return PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: displayName,
                skillLevel: skillLevel,
                city: "Amsterdam",
                bio: "Participant read model test profile.");
        }
    }
}