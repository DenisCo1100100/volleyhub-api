using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Queries.GetGameParticipants;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Queries.GetGameParticipants
{
    public sealed class GetGameParticipantsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnParticipantDtos_WhenParticipantsExist()
        {
            var gameId = Guid.NewGuid();

            var participants = new List<GameParticipant>
            {
                GameParticipant.JoinOpenGame(
                    gameId: gameId,
                    playerProfileId: Guid.NewGuid(),
                    joinedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
                    offlinePaymentStatus: GameParticipantOfflinePaymentStatus.NotRequired),

                GameParticipant.RequestToJoin(
                    gameId: gameId,
                    playerProfileId: Guid.NewGuid(),
                    joinedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
                    offlinePaymentStatus: GameParticipantOfflinePaymentStatus.Pending)
            };

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(participants);

            var handler = new GetGameParticipantsQueryHandler(
                gameParticipantRepositoryMock.Object);

            var query = new GetGameParticipantsQuery(gameId);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(2);

            result[0].Id.Should().Be(participants[0].Id);
            result[0].GameId.Should().Be(participants[0].GameId);
            result[0].PlayerProfileId.Should().Be(participants[0].PlayerProfileId);
            result[0].JoinedAt.Should().Be(participants[0].JoinedAt);
            result[0].ApprovedAt.Should().Be(participants[0].ApprovedAt);
            result[0].JoinStatus.Should().Be(participants[0].JoinStatus);
            result[0].AttendanceStatus.Should().Be(participants[0].AttendanceStatus);
            result[0].OfflinePaymentStatus.Should().Be(participants[0].OfflinePaymentStatus);

            result[1].Id.Should().Be(participants[1].Id);
            result[1].GameId.Should().Be(participants[1].GameId);
            result[1].PlayerProfileId.Should().Be(participants[1].PlayerProfileId);
            result[1].JoinedAt.Should().Be(participants[1].JoinedAt);
            result[1].ApprovedAt.Should().Be(participants[1].ApprovedAt);
            result[1].JoinStatus.Should().Be(participants[1].JoinStatus);
            result[1].AttendanceStatus.Should().Be(participants[1].AttendanceStatus);
            result[1].OfflinePaymentStatus.Should().Be(participants[1].OfflinePaymentStatus);

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenParticipantsDoNotExist()
        {
            var gameId = Guid.NewGuid();

            var gameParticipantRepositoryMock = new Mock<IGameParticipantRepository>();

            gameParticipantRepositoryMock
                .Setup(repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GameParticipant>());

            var handler = new GetGameParticipantsQueryHandler(
                gameParticipantRepositoryMock.Object);

            var query = new GetGameParticipantsQuery(gameId);

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().BeEmpty();

            gameParticipantRepositoryMock.Verify(
                repository => repository.GetByGameIdAsync(
                    gameId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}