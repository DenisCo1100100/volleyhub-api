using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.Games.Queries.GetPlayerGameHistory;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Games.Queries.GameHistory
{
    public sealed class GetPlayerGameHistoryQueryHandlerTests
    {
        private readonly Mock<IGameRepository> _games = new(MockBehavior.Strict);
        private readonly Mock<IPlayerProfileRepository> _profiles = new(MockBehavior.Strict);
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IDateTimeProvider> _clock = new();

        [Fact]
        public async Task Handle_ShouldScopeToCurrentProfileAndForwardFiltersClockAndCancellation()
        {
            var userId = Guid.NewGuid();
            var profile = PlayerProfile.Create(userId, "Player", PlayerSkillLevel.Intermediate, "Minsk", null);
            var now = DateTimeOffset.UtcNow;
            var from = now.AddDays(-5).ToOffset(TimeSpan.FromHours(3));
            var to = now.AddDays(5).ToOffset(TimeSpan.FromHours(3));
            var courtId = Guid.NewGuid();
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            _currentUser.SetupGet(service => service.UserId).Returns(userId);
            _clock.SetupGet(service => service.UtcNow).Returns(now);
            _profiles.Setup(repository => repository.GetByUserIdAsync(userId, token)).ReturnsAsync(profile);
            var expected = new PagedResult<PlayerGameHistoryDto>([], 2, 10, 0);
            var parameters = new GameHistoryQueryParameters(profile.Id, now, 2, 10, GameHistoryPeriod.Past,
                GameStatus.Completed, from.ToUniversalTime(), to.ToUniversalTime(), courtId, GameParticipantJoinStatus.Approved);
            _games.Setup(repository => repository.GetPlayerHistoryAsync(parameters, token)).ReturnsAsync(expected);

            var result = await CreateHandler().Handle(new GetPlayerGameHistoryQuery(2, 10, GameHistoryPeriod.Past,
                GameStatus.Completed, from, to, courtId, GameParticipantJoinStatus.Approved), token);

            result.Should().BeSameAs(expected);
            _games.VerifyAll();
            _profiles.VerifyAll();
        }

        [Fact]
        public async Task Handle_ShouldRejectAnonymousUserBeforeReadingData()
        {
            Func<Task> action = () => CreateHandler().Handle(new GetPlayerGameHistoryQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<UnauthorizedException>();
            _games.VerifyNoOtherCalls();
            _profiles.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Handle_ShouldRejectMissingOrDeletedProfile(bool deleted)
        {
            var userId = Guid.NewGuid();
            var profile = PlayerProfile.Create(userId, "Player", PlayerSkillLevel.Intermediate, null, null);
            profile.Delete();
            _currentUser.SetupGet(service => service.UserId).Returns(userId);
            _profiles.Setup(repository => repository.GetByUserIdAsync(userId, CancellationToken.None))
                .ReturnsAsync(deleted ? profile : null);

            Func<Task> action = () => CreateHandler().Handle(new GetPlayerGameHistoryQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<NotFoundException>();
            _games.VerifyNoOtherCalls();
        }

        private GetPlayerGameHistoryQueryHandler CreateHandler() => new(_games.Object, _profiles.Object, _currentUser.Object, _clock.Object);
    }
}
