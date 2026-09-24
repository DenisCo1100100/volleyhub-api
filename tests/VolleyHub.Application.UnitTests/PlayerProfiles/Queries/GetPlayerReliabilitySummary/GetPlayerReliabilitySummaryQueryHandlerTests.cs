using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetPlayerReliabilitySummary
{
    public sealed class GetPlayerReliabilitySummaryQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnFactualSummaryAndForwardCancellationToken()
        {
            var player = PlayerProfile.Create(Guid.NewGuid(), "Player", PlayerSkillLevel.Intermediate, null, null);
            var summary = new PlayerReliabilitySummaryDto(player.Id, 3, 1, 2, 4, 5);
            using var cancellation = new CancellationTokenSource();
            var profiles = new Mock<IPlayerProfileRepository>();
            var participants = new Mock<IGameParticipantRepository>();
            profiles.Setup(r => r.GetByIdAsync(player.Id, cancellation.Token)).ReturnsAsync(player);
            participants.Setup(r => r.GetReliabilitySummaryAsync(player.Id, cancellation.Token)).ReturnsAsync(summary);
            var handler = new GetPlayerReliabilitySummaryQueryHandler(profiles.Object, participants.Object);

            var result = await handler.Handle(new GetPlayerReliabilitySummaryQuery(player.Id), cancellation.Token);

            result.Should().BeSameAs(summary);
            participants.Verify(r => r.GetReliabilitySummaryAsync(player.Id, cancellation.Token), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Handle_ShouldRejectMissingOrDeletedProfileWithoutReadingReliability(bool deleted)
        {
            var player = PlayerProfile.Create(Guid.NewGuid(), "Player", PlayerSkillLevel.Intermediate, null, null);
            if (deleted) player.Delete();
            var profiles = new Mock<IPlayerProfileRepository>();
            var participants = new Mock<IGameParticipantRepository>();
            profiles.Setup(r => r.GetByIdAsync(player.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deleted ? player : null);
            var handler = new GetPlayerReliabilitySummaryQueryHandler(profiles.Object, participants.Object);

            var act = () => handler.Handle(new GetPlayerReliabilitySummaryQuery(player.Id), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
            participants.Verify(r => r.GetReliabilitySummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(3, 1, 75)]
        [InlineData(1, 2, 33.33)]
        [InlineData(2, 1, 66.67)]
        [InlineData(1, 0, 100)]
        [InlineData(0, 2, 0)]
        public void Summary_ShouldUseOnlyMarkedAttendanceForRate(int attended, int noShows, decimal expectedRate)
        {
            var summary = new PlayerReliabilitySummaryDto(Guid.NewGuid(), attended, noShows, 7, 8, 9);

            summary.TotalMarkedGamesCount.Should().Be(attended + noShows);
            summary.AttendanceRate.Should().Be(expectedRate);
            summary.LateCancellationCount.Should().Be(7);
            summary.OnTimeCancellationCount.Should().Be(8);
            summary.UnmarkedGamesCount.Should().Be(9);
        }
    }
}