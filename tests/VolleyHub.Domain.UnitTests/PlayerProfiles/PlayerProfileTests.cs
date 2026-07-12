using FluentAssertions;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Domain.UnitTests.PlayerProfiles
{
    public sealed class PlayerProfileTests
    {
        [Fact]
        public void Create_ShouldCreatePlayerProfile_WhenDataIsValid()
        {
            var userId = Guid.NewGuid();

            var playerProfile = PlayerProfile.Create(
                userId: userId,
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Kyiv",
                bio: "I like beach volleyball.");

            playerProfile.Id.Should().NotBeEmpty();
            playerProfile.UserId.Should().Be(userId);
            playerProfile.DisplayName.Should().Be("John Player");
            playerProfile.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);
            playerProfile.City.Should().Be("Kyiv");
            playerProfile.Bio.Should().Be("I like beach volleyball.");
            playerProfile.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public void Create_ShouldTrimTextFields_WhenTextContainsWhitespaces()
        {
            var playerProfile = PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "  John Player  ",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "  Kyiv  ",
                bio: "  I like beach volleyball.  ");

            playerProfile.DisplayName.Should().Be("John Player");
            playerProfile.City.Should().Be("Kyiv");
            playerProfile.Bio.Should().Be("I like beach volleyball.");
        }

        [Fact]
        public void Create_ShouldSetOptionalFieldsToNull_WhenOptionalFieldsAreWhiteSpace()
        {
            var playerProfile = PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "   ",
                bio: "   ");

            playerProfile.City.Should().BeNull();
            playerProfile.Bio.Should().BeNull();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            Action act = () => PlayerProfile.Create(
                userId: Guid.Empty,
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Kyiv",
                bio: null);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_ShouldThrowArgumentException_WhenDisplayNameIsInvalid(string? displayName)
        {
            Action act = () => PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: displayName!,
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Kyiv",
                bio: null);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenDisplayNameIsTooLong()
        {
            var displayName = new string('a', PlayerProfile.MaxDisplayNameLength + 1);

            Action act = () => PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: displayName,
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Kyiv",
                bio: null);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(PlayerSkillLevel.Unknown)]
        [InlineData((PlayerSkillLevel)999)]
        public void Create_ShouldThrowArgumentException_WhenSkillLevelIsInvalid(PlayerSkillLevel skillLevel)
        {
            Action act = () => PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: skillLevel,
                city: "Kyiv",
                bio: null);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenCityIsTooLong()
        {
            var city = new string('a', PlayerProfile.MaxCityLength + 1);

            Action act = () => PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: city,
                bio: null);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenBioIsTooLong()
        {
            var bio = new string('a', PlayerProfile.MaxBioLength + 1);

            Action act = () => PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Kyiv",
                bio: bio);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void UpdateDetails_ShouldUpdatePlayerProfile_WhenDataIsValid()
        {
            var playerProfile = CreatePlayerProfile();

            playerProfile.UpdateDetails(
                displayName: "Updated Player",
                skillLevel: PlayerSkillLevel.Advanced,
                city: "Lviv",
                bio: "Updated bio.");

            playerProfile.DisplayName.Should().Be("Updated Player");
            playerProfile.SkillLevel.Should().Be(PlayerSkillLevel.Advanced);
            playerProfile.City.Should().Be("Lviv");
            playerProfile.Bio.Should().Be("Updated bio.");
        }

        [Fact]
        public void Delete_ShouldMarkPlayerProfileAsDeleted()
        {
            var playerProfile = CreatePlayerProfile();

            playerProfile.Delete();

            playerProfile.IsDeleted.Should().BeTrue();
        }

        private static PlayerProfile CreatePlayerProfile()
        {
            return PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Kyiv",
                bio: "I like beach volleyball.");
        }
    }
}