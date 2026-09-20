using FluentAssertions;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.Games.Queries.GetPlayerGameHistory;
using VolleyHub.Application.Games.Queries.GetOrganizedGameHistory;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.Games.Queries.GameHistory
{
    public sealed class GameHistoryQueryValidatorTests
    {
        [Fact]
        public void Validate_ShouldAcceptDefaultsAndInclusiveDateRange()
        {
            ValidateBoth(new GetPlayerGameHistoryQuery(), true);
            var now = DateTimeOffset.UtcNow;
            ValidateBoth(new GetPlayerGameHistoryQuery(2, 100, GameHistoryPeriod.Past, GameStatus.Completed, now, now, Guid.NewGuid()), true);
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(-1, 20)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        [InlineData(int.MaxValue, 100)]
        public void Validate_ShouldRejectInvalidPaginationAndOverflow(int page, int pageSize)
        {
            ValidateBoth(new GetPlayerGameHistoryQuery(Page: page, PageSize: pageSize), false);
        }

        [Fact]
        public void Validate_ShouldRejectInvalidFilters()
        {
            ValidateBoth(new GetPlayerGameHistoryQuery(Period: (GameHistoryPeriod)999), false);
            ValidateBoth(new GetPlayerGameHistoryQuery(Status: (GameStatus)999), false);
            ValidateBoth(new GetPlayerGameHistoryQuery(CourtId: Guid.Empty), false);
            var now = DateTimeOffset.UtcNow;
            ValidateBoth(new GetPlayerGameHistoryQuery(StartsAtFrom: now.AddSeconds(1), StartsAtTo: now), false);
        }

        [Theory]
        [InlineData(GameParticipantJoinStatus.Unknown, false)]
        [InlineData((GameParticipantJoinStatus)999, false)]
        [InlineData(GameParticipantJoinStatus.Cancelled, true)]
        [InlineData(GameParticipantJoinStatus.Removed, true)]
        [InlineData(GameParticipantJoinStatus.Waitlisted, true)]
        public void Validate_ShouldValidateParticipationFilter(GameParticipantJoinStatus status, bool valid)
        {
            new GetPlayerGameHistoryQueryValidator().Validate(new GetPlayerGameHistoryQuery(JoinStatus: status)).IsValid.Should().Be(valid);
        }

        private static void ValidateBoth(GetPlayerGameHistoryQuery query, bool valid)
        {
            new GetPlayerGameHistoryQueryValidator().Validate(query).IsValid.Should().Be(valid);
            new GetOrganizedGameHistoryQueryValidator().Validate(new GetOrganizedGameHistoryQuery(query.Page, query.PageSize,
                query.Period, query.Status, query.StartsAtFrom, query.StartsAtTo, query.CourtId)).IsValid.Should().Be(valid);
        }
    }
}
