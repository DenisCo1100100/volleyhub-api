using FluentValidation.TestHelper;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetPlayerReliabilitySummary
{
    public sealed class GetPlayerReliabilitySummaryQueryValidatorTests
    {
        private readonly GetPlayerReliabilitySummaryQueryValidator _validator = new();

        [Fact]
        public void Validate_ShouldNotHaveValidationErrors_WhenQueryIsValid()
        {
            var query = new GetPlayerReliabilitySummaryQuery(Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenPlayerProfileIdIsEmpty()
        {
            var query = new GetPlayerReliabilitySummaryQuery(Guid.Empty);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(query => query.PlayerProfileId);
        }
    }
}