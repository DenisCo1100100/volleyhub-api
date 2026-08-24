using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.Games.Queries.GetGames;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldPassPaginationFiltersAndCurrentUserToRepository()
        {
            var currentUserId = Guid.NewGuid();
            var courtId = Guid.NewGuid();
            var startsAtFrom = DateTimeOffset.UtcNow.AddDays(1);
            var startsAtTo = startsAtFrom.AddDays(7);

            var expectedResult = new PagedResult<GameSummaryDto>(
                new List<GameSummaryDto>
                {
                    CreateGameSummary()
                },
                Page: 2,
                PageSize: 10,
                TotalCount: 21);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            gameRepositoryMock
                .Setup(repository => repository.GetSummariesAsync(
                    It.Is<GameSummaryQueryParameters>(parameters =>
                        parameters.Page == 2
                        && parameters.PageSize == 10
                        && parameters.StartsAtFrom == startsAtFrom
                        && parameters.StartsAtTo == startsAtTo
                        && parameters.CourtId == courtId
                        && parameters.Status == GameStatus.Open
                        && parameters.CurrentUserId == currentUserId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                currentUserServiceMock.Object);

            var result = await handler.Handle(
                new GetGamesQuery(
                    Page: 2,
                    PageSize: 10,
                    StartsAtFrom: startsAtFrom,
                    StartsAtTo: startsAtTo,
                    CourtId: courtId,
                    Status: GameStatus.Open),
                CancellationToken.None);

            result.Should().BeSameAs(expectedResult);
            result.Items.Should().ContainSingle();
            result.Page.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(21);
            result.TotalPages.Should().Be(3);

            gameRepositoryMock.Verify(
                repository => repository.GetSummariesAsync(
                    It.IsAny<GameSummaryQueryParameters>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUseDefaultPaginationAndNoCurrentUser_WhenRequestIsAnonymous()
        {
            var expectedResult = new PagedResult<GameSummaryDto>(
                Array.Empty<GameSummaryDto>(),
                Page: 1,
                PageSize: 20,
                TotalCount: 0);

            var gameRepositoryMock = new Mock<IGameRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            gameRepositoryMock
                .Setup(repository => repository.GetSummariesAsync(
                    It.Is<GameSummaryQueryParameters>(parameters =>
                        parameters.Page == 1
                        && parameters.PageSize == 20
                        && parameters.StartsAtFrom == null
                        && parameters.StartsAtTo == null
                        && parameters.CourtId == null
                        && parameters.Status == null
                        && parameters.CurrentUserId == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var handler = new GetGamesQueryHandler(
                gameRepositoryMock.Object,
                currentUserServiceMock.Object);

            var result = await handler.Handle(
                new GetGamesQuery(),
                CancellationToken.None);

            result.Should().BeSameAs(expectedResult);
            result.Items.Should().BeEmpty();
            result.TotalPages.Should().Be(0);
        }

        private static GameSummaryDto CreateGameSummary()
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return new GameSummaryDto(
                Guid.NewGuid(),
                new GameCourtSummaryDto(
                    Guid.NewGuid(),
                    "Central Court",
                    "Test Street 10",
                    52.3676,
                    4.9041,
                    CourtSurfaceType.Indoor,
                    true),
                new GameOrganizerSummaryDto(
                    Guid.NewGuid(),
                    "Organizer",
                    PlayerSkillLevel.Intermediate),
                startsAt,
                startsAt.AddHours(2),
                12,
                15,
                GameLevel.Intermediate,
                GameJoinPolicy.ApprovalRequired,
                GameStatus.Open,
                4,
                2,
                8,
                GameParticipantJoinStatus.PendingApproval);
        }
    }
}