using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Queries.GetCourts;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.UnitTests.Courts.Queries.GetCourts
{
    public sealed class GetCourtsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnCourtDtos_WhenCourtsExist()
        {
            // Arrange
            var courts = new List<Court>
            {
                Court.Create(
                    name: "Central Beach Court",
                    address: "Kyiv, Hydropark",
                    latitude: 50.4547,
                    longitude: 30.5861,
                    surfaceType: CourtSurfaceType.Sand,
                    isIndoor: false,
                    description: "Public beach volleyball court"),

                Court.Create(
                    name: "Indoor Arena",
                    address: "Kyiv, Sports Complex",
                    latitude: 50.4501,
                    longitude: 30.5234,
                    surfaceType: CourtSurfaceType.Rubber,
                    isIndoor: true,
                    description: "Indoor volleyball court")
            };

            var courtRepositoryMock = new Mock<ICourtRepository>();

            courtRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(courts);

            var handler = new GetCourtsQueryHandler(courtRepositoryMock.Object);

            var query = new GetCourtsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);

            result[0].Id.Should().Be(courts[0].Id);
            result[0].Name.Should().Be(courts[0].Name);
            result[0].Address.Should().Be(courts[0].Address);
            result[0].SurfaceType.Should().Be(courts[0].SurfaceType);

            result[1].Id.Should().Be(courts[1].Id);
            result[1].Name.Should().Be(courts[1].Name);
            result[1].Address.Should().Be(courts[1].Address);
            result[1].SurfaceType.Should().Be(courts[1].SurfaceType);

            courtRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenCourtsDoNotExist()
        {
            // Arrange
            var courtRepositoryMock = new Mock<ICourtRepository>();

            courtRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Court>());

            var handler = new GetCourtsQueryHandler(courtRepositoryMock.Object);

            var query = new GetCourtsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();

            courtRepositoryMock.Verify(
                repository => repository.GetListAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}