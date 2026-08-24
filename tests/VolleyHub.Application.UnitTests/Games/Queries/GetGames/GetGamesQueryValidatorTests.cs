using FluentAssertions;
using VolleyHub.Application.Games.Queries.GetGames;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.Games.Queries.GetGames
{
    public sealed class GetGamesQueryValidatorTests
    {
        private readonly GetGamesQueryValidator _validator = new();

        [Fact]
        public void Validate_ShouldSucceed_WhenQueryIsValid()
        {
            var query = new GetGamesQuery(
                Page: 2,
                PageSize: 50,
                StartsAtFrom: DateTimeOffset.UtcNow,
                StartsAtTo: DateTimeOffset.UtcNow.AddDays(7),
                CourtId: Guid.NewGuid(),
                Status: GameStatus.Open);

            var result = _validator.Validate(query);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_ShouldFail_WhenPageIsInvalid(int page)
        {
            var result = _validator.Validate(
                new GetGamesQuery(Page: page));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(GetGamesQuery.Page));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Validate_ShouldFail_WhenPageSizeIsInvalid(int pageSize)
        {
            var result = _validator.Validate(
                new GetGamesQuery(PageSize: pageSize));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(GetGamesQuery.PageSize));
        }

        [Fact]
        public void Validate_ShouldFail_WhenCourtIdIsEmpty()
        {
            var result = _validator.Validate(
                new GetGamesQuery(CourtId: Guid.Empty));

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ShouldFail_WhenStatusIsInvalid()
        {
            var result = _validator.Validate(
                new GetGamesQuery(Status: (GameStatus)999));

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ShouldFail_WhenStartsAtFromIsAfterStartsAtTo()
        {
            var startsAtTo = DateTimeOffset.UtcNow.AddDays(1);
            var startsAtFrom = startsAtTo.AddDays(1);

            var result = _validator.Validate(
                new GetGamesQuery(
                    StartsAtFrom: startsAtFrom,
                    StartsAtTo: startsAtTo));

            result.IsValid.Should().BeFalse();
        }
    }
}