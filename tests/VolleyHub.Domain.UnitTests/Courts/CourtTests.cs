using FluentAssertions;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Domain.UnitTests.Courts
{
    public sealed class CourtTests
    {
        [Fact]
        public void Create_ShouldCreateCourt_WhenDataIsValid()
        {
            // Act
            var court = Court.Create(
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "Public beach volleyball court");

            // Assert
            court.Id.Should().NotBeEmpty();
            court.Name.Should().Be("Central Beach Court");
            court.Address.Should().Be("Kyiv, Hydropark");
            court.Latitude.Should().Be(50.4547);
            court.Longitude.Should().Be(30.5861);
            court.SurfaceType.Should().Be(CourtSurfaceType.Sand);
            court.IsIndoor.Should().BeFalse();
            court.Description.Should().Be("Public beach volleyball court");
            court.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public void Create_ShouldTrimRequiredTextFields_WhenTextContainsWhitespaces()
        {
            // Act
            var court = Court.Create(
                name: "  Central Beach Court  ",
                address: "  Kyiv, Hydropark  ",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "  Public beach volleyball court  ");

            // Assert
            court.Name.Should().Be("Central Beach Court");
            court.Address.Should().Be("Kyiv, Hydropark");
            court.Description.Should().Be("Public beach volleyball court");
        }

        [Fact]
        public void Create_ShouldSetDescriptionToNull_WhenDescriptionIsWhiteSpace()
        {
            // Act
            var court = Court.Create(
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "   ");

            // Assert
            court.Description.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_ShouldThrowArgumentNullException_WhenNameIsInvalid(string? name)
        {
            // Act
            Action act = () => Court.Create(
                name: name!,
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_ShouldThrowArgumentNullException_WhenAddressIsInvalid(string? address)
        {
            // Act
            Action act = () => Court.Create(
                name: "Central Beach Court",
                address: address!,
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        public void Create_ShouldThrowArgumentException_WhenLatitudeIsOutOfRange(double latitude)
        {
            // Act
            Action act = () => Court.Create(
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: latitude,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        public void Create_ShouldThrowArgumentException_WhenLongitudeIsOutOfRange(double longitude)
        {
            // Act
            Action act = () => Court.Create(
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: longitude,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_ShouldUpdateCourtDetails_WhenDataIsValid()
        {
            // Arrange
            var court = Court.Create(
                name: "Old Court",
                address: "Old Address",
                latitude: 10,
                longitude: 20,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "Old description");

            // Act
            court.Update(
                name: "New Court",
                address: "New Address",
                latitude: 30,
                longitude: 40,
                surfaceType: CourtSurfaceType.Rubber,
                isIndoor: true,
                description: "New description");

            // Assert
            court.Name.Should().Be("New Court");
            court.Address.Should().Be("New Address");
            court.Latitude.Should().Be(30);
            court.Longitude.Should().Be(40);
            court.SurfaceType.Should().Be(CourtSurfaceType.Rubber);
            court.IsIndoor.Should().BeTrue();
            court.Description.Should().Be("New description");
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenNameIsTooLong()
        {
            // Arrange
            var name = new string('a', Court.MaxNameLength + 1);

            // Act
            Action act = () => Court.Create(
                name: name,
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenAddressIsTooLong()
        {
            // Arrange
            var address = new string('a', Court.MaxAddressLength + 1);

            // Act
            Action act = () => Court.Create(
                name: "Central Beach Court",
                address: address,
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenDescriptionIsTooLong()
        {
            // Arrange
            var description = new string('a', Court.MaxDescriptionLength + 1);

            // Act
            Action act = () => Court.Create(
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: description);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Delete_ShouldMarkCourtAsDeleted()
        {
            // Arrange
            var court = Court.Create(
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            // Act
            court.Delete();

            // Assert
            court.IsDeleted.Should().BeTrue();
        }
    }
}