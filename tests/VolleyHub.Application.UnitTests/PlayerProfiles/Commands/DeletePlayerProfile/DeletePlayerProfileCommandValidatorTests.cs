using FluentValidation.TestHelper;
using VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Commands.DeletePlayerProfile
{
    public sealed class DeletePlayerProfileCommandValidatorTests
    {
        private readonly DeletePlayerProfileCommandValidator _validator = new();

        [Fact]
        public void Validate_ShouldNotHaveValidationErrors_WhenCommandIsValid()
        {
            var command = new DeletePlayerProfileCommand(Guid.NewGuid());

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenIdIsEmpty()
        {
            var command = new DeletePlayerProfileCommand(Guid.Empty);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.Id);
        }
    }
}