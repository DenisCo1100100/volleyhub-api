using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Queries.GetCourtById;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.UnitTests.Courts.Queries.GetCourtById
{
    public sealed class GetCourtByIdQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnCourtDto_WhenCourtExists()
        {
            // Arrange
            var court = CreateCourt();

            var courtRepositoryMock = new Mock<ICourtRepository>();

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(court);

            var handler = new GetCourtByIdQueryHandler(courtRepositoryMock.Object);

            var query = new GetCourtByIdQuery(court.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Id.Should().Be(court.Id);
            result.Name.Should().Be(court.Name);
            result.Address.Should().Be(court.Address);
            result.Latitude.Should().Be(court.Latitude);
            result.Longitude.Should().Be(court.Longitude);
            result.SurfaceType.Should().Be(court.SurfaceType);
            result.IsIndoor.Should().Be(court.IsIndoor);
            result.Description.Should().Be(court.Description);

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCourtDoesNotExist()
        {
            // Arrange
            var courtId = Guid.NewGuid();

            var courtRepositoryMock = new Mock<ICourtRepository>();

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    courtId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Court?)null);

            var handler = new GetCourtByIdQueryHandler(courtRepositoryMock.Object);

            var query = new GetCourtByIdQuery(courtId);

            // Act
            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    courtId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static Court CreateCourt()
        {
            return Court.Create(
                ownerPlayerProfileId: Guid.NewGuid(),
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
