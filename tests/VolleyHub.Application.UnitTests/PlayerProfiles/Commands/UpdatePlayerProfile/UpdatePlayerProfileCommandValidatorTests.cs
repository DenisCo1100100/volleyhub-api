using FluentValidation.TestHelper;
using VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Commands.UpdatePlayerProfile
{
    public sealed class UpdatePlayerProfileCommandValidatorTests
    {
        private readonly UpdatePlayerProfileCommandValidator _validator = new();

        [Fact]
        public void Validate_ShouldNotHaveValidationErrors_WhenCommandIsValid()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "John Player",
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenIdIsEmpty()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.Empty,
                DisplayName: "John Player",
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.Id);
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenDisplayNameIsEmpty()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: string.Empty,
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.DisplayName);
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenDisplayNameIsTooLong()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: new string('a', PlayerProfile.MaxDisplayNameLength + 1),
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.DisplayName);
        }

        [Theory]
        [InlineData(PlayerSkillLevel.Unknown)]
        [InlineData((PlayerSkillLevel)999)]
        public void Validate_ShouldHaveValidationError_WhenSkillLevelIsInvalid(PlayerSkillLevel skillLevel)
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "John Player",
                SkillLevel: skillLevel,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.SkillLevel);
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenCityIsTooLong()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "John Player",
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: new string('a', PlayerProfile.MaxCityLength + 1),
                Bio: "I like volleyball.");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.City);
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenBioIsTooLong()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "John Player",
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: new string('a', PlayerProfile.MaxBioLength + 1));

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.Bio);
        }
    }
}