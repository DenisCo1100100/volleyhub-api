using FluentValidation.TestHelper;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetPlayerProfileById
{
    public sealed class GetPlayerProfileByIdQueryValidatorTests
    {
        private readonly GetPlayerProfileByIdQueryValidator _validator = new();

        [Fact]
        public void Validate_ShouldNotHaveValidationErrors_WhenQueryIsValid()
        {
            var query = new GetPlayerProfileByIdQuery(Guid.NewGuid());

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenIdIsEmpty()
        {
            var query = new GetPlayerProfileByIdQuery(Guid.Empty);

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(query => query.Id);
        }
    }
}