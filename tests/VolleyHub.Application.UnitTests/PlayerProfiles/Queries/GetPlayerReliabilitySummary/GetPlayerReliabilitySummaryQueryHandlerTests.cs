using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetPlayerReliabilitySummary
{
    public sealed class GetPlayerReliabilitySummaryQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnReliabilitySummary_WhenPlayerHasMarkedGames()
        {
            var playerProfile = CreatePlayerProfile();

            var participants = new List<GameParticipant>
            {
                CreateParticipantWithAttendance(playerProfile.Id, GameParticipantAttendanceStatus.Present),
                CreateParticipantWithAttendance(playerProfile.Id, GameParticipantAttendanceStatus.Present),
                CreateParticipantWithAttendance(playerProfile.Id, GameParticipantAttendanceStatus.Present),
                CreateParticipantWithAttendance(playerProfile.Id, GameParticipantAttendanceStatus.Absent),
                CreateParticipantWithoutMarkedAttendance(playerProfile.Id)
            };

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByPlayerProfileIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            var handler = new GetPlayerReliabilitySummaryQueryHandler(
                playerProfileRepositoryMock.Object,
                gameParticipantRepositoryMock.Object);

            var query = new GetPlayerReliabilitySummaryQuery(playerProfile.Id);

            var result = await handler.Handle(query, CancellationToken.None);

            result.PlayerProfileId.Should().Be(playerProfile.Id);
            result.AttendedGamesCount.Should().Be(3);
            result.NoShowCount.Should().Be(1);
            result.LateCancellationCount.Should().Be(0);
            result.TotalMarkedGamesCount.Should().Be(4);
            result.AttendanceRate.Should().Be(75);
        }

        [Fact]
        public async Task Handle_ShouldReturnZeroSummary_WhenPlayerHasNoMarkedGames()
        {
            var playerProfile = CreatePlayerProfile();

            var participants = new List<GameParticipant>
            {
                CreateParticipantWithoutMarkedAttendance(playerProfile.Id)
            };

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByPlayerProfileIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            var handler = new GetPlayerReliabilitySummaryQueryHandler(
                playerProfileRepositoryMock.Object,
                gameParticipantRepositoryMock.Object);

            var query = new GetPlayerReliabilitySummaryQuery(playerProfile.Id);

            var result = await handler.Handle(query, CancellationToken.None);

            result.PlayerProfileId.Should().Be(playerProfile.Id);
            result.AttendedGamesCount.Should().Be(0);
            result.NoShowCount.Should().Be(0);
            result.LateCancellationCount.Should().Be(0);
            result.TotalMarkedGamesCount.Should().Be(0);
            result.AttendanceRate.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenPlayerProfileDoesNotExist()
        {
            var playerProfileId = Guid.NewGuid();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfileId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new GetPlayerReliabilitySummaryQueryHandler(
                playerProfileRepositoryMock.Object,
                gameParticipantRepositoryMock.Object);

            var query = new GetPlayerReliabilitySummaryQuery(playerProfileId);

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByPlayerProfileIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenPlayerProfileIsDeleted()
        {
            var playerProfile = CreatePlayerProfile();
            playerProfile.Delete();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new GetPlayerReliabilitySummaryQueryHandler(
                playerProfileRepositoryMock.Object,
                gameParticipantRepositoryMock.Object);

            var query = new GetPlayerReliabilitySummaryQuery(playerProfile.Id);

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByPlayerProfileIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static PlayerProfile CreatePlayerProfile()
        {
            return PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "I like volleyball.");
        }

        private static GameParticipant CreateParticipantWithAttendance(
            Guid playerProfileId,
            GameParticipantAttendanceStatus attendanceStatus)
        {
            var participant = CreateParticipantWithoutMarkedAttendance(playerProfileId);

            participant.MarkAttendance(attendanceStatus);

            return participant;
        }

        private static GameParticipant CreateParticipantWithoutMarkedAttendance(Guid playerProfileId)
        {
            return GameParticipant.JoinOpenGame(
                gameId: Guid.NewGuid(),
                playerProfileId: playerProfileId,
                joinedAt: DateTimeOffset.UtcNow.AddDays(-1),
                offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending);
        }
    }
}