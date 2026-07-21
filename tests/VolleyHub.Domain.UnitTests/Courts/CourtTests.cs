using FluentAssertions;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Domain.UnitTests.Courts
{
    public sealed class CourtTests
    {
        [Fact]
        public void Create_ShouldCreateCourt_WhenDataIsValid()
        {
            var ownerPlayerProfileId = Guid.NewGuid();

            var court = CreateCourt(ownerPlayerProfileId);

            court.Id.Should().NotBeEmpty();

            court.OwnerPlayerProfileId.Should()
                .Be(ownerPlayerProfileId);

            court.Name.Should()
                .Be("Central Beach Court");

            court.Address.Should()
                .Be("Kyiv, Hydropark");

            court.Latitude.Should()
                .Be(50.4547);

            court.Longitude.Should()
                .Be(30.5861);

            court.SurfaceType.Should()
                .Be(CourtSurfaceType.Sand);

            court.IsIndoor.Should()
                .BeFalse();

            court.Description.Should()
                .Be("Public beach volleyball court");

            court.IsDeleted.Should()
                .BeFalse();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenOwnerPlayerProfileIdIsEmpty()
        {
            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.Empty,
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentException>()
                .WithParameterName("ownerPlayerProfileId");
        }

        [Fact]
        public void IsOwnedBy_ShouldReturnTrue_WhenPlayerProfileIsOwner()
        {
            var ownerPlayerProfileId = Guid.NewGuid();
            var court = CreateCourt(ownerPlayerProfileId);

            var result = court.IsOwnedBy(
                ownerPlayerProfileId);

            result.Should().BeTrue();
        }

        [Fact]
        public void IsOwnedBy_ShouldReturnFalse_WhenPlayerProfileIsNotOwner()
        {
            var court = CreateCourt(
                Guid.NewGuid());

            var result = court.IsOwnedBy(
                Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public void IsOwnedBy_ShouldReturnFalse_WhenPlayerProfileIdIsEmpty()
        {
            var court = CreateCourt(
                Guid.NewGuid());

            var result = court.IsOwnedBy(
                Guid.Empty);

            result.Should().BeFalse();
        }

        [Fact]
        public void Create_ShouldTrimRequiredTextFields_WhenTextContainsWhitespaces()
        {
            var court = Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "  Central Beach Court  ",
                address: "  Kyiv, Hydropark  ",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "  Public beach volleyball court  ");

            court.Name.Should()
                .Be("Central Beach Court");

            court.Address.Should()
                .Be("Kyiv, Hydropark");

            court.Description.Should()
                .Be("Public beach volleyball court");
        }

        [Fact]
        public void Create_ShouldSetDescriptionToNull_WhenDescriptionIsWhiteSpace()
        {
            var court = Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "   ");

            court.Description.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_ShouldThrowArgumentNullException_WhenNameIsInvalid(
            string? name)
        {
            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: name!,
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_ShouldThrowArgumentNullException_WhenAddressIsInvalid(
            string? address)
        {
            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "Central Beach Court",
                address: address!,
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        public void Create_ShouldThrowArgumentException_WhenLatitudeIsOutOfRange(
            double latitude)
        {
            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: latitude,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        public void Create_ShouldThrowArgumentException_WhenLongitudeIsOutOfRange(
            double longitude)
        {
            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: longitude,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void Update_ShouldUpdateCourtDetails_WhenDataIsValid()
        {
            var ownerPlayerProfileId = Guid.NewGuid();
            var court = CreateCourt(ownerPlayerProfileId);

            court.Update(
                name: "New Court",
                address: "New Address",
                latitude: 30,
                longitude: 40,
                surfaceType: CourtSurfaceType.Rubber,
                isIndoor: true,
                description: "New description");

            court.OwnerPlayerProfileId.Should()
                .Be(ownerPlayerProfileId);

            court.Name.Should()
                .Be("New Court");

            court.Address.Should()
                .Be("New Address");

            court.Latitude.Should()
                .Be(30);

            court.Longitude.Should()
                .Be(40);

            court.SurfaceType.Should()
                .Be(CourtSurfaceType.Rubber);

            court.IsIndoor.Should()
                .BeTrue();

            court.Description.Should()
                .Be("New description");
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenNameIsTooLong()
        {
            var name = new string(
                'a',
                Court.MaxNameLength + 1);

            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: name,
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenAddressIsTooLong()
        {
            var address = new string(
                'a',
                Court.MaxAddressLength + 1);

            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "Central Beach Court",
                address: address,
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: null);

            act.Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenDescriptionIsTooLong()
        {
            var description = new string(
                'a',
                Court.MaxDescriptionLength + 1);

            Action act = () => Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: description);

            act.Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void Delete_ShouldMarkCourtAsDeleted()
        {
            var court = CreateCourt(
                Guid.NewGuid());

            court.Delete();

            court.IsDeleted.Should().BeTrue();
        }

        private static Court CreateCourt(
            Guid ownerPlayerProfileId)
        {
            return Court.Create(
                ownerPlayerProfileId: ownerPlayerProfileId,
                name: "Central Beach Court",
                address: "Kyiv, Hydropark",
                latitude: 50.4547,
                longitude: 30.5861,
                surfaceType: CourtSurfaceType.Sand,
                isIndoor: false,
                description: "Public beach volleyball court");
        }
    }
}