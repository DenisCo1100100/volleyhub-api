using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Commands.CreateCourt;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.UnitTests.Courts.Commands.CreateCourt
{
    public sealed class CreateCourtCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldAddCourtAndSaveChanges_WhenCommandIsValid()
        {
            // Arrange
            var courtRepositoryMock = new Mock<ICourtRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            Court? addedCourt = null;

            courtRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Court>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Court, CancellationToken>((court, _) => addedCourt = court)
                .Returns(Task.CompletedTask);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new CreateCourtCommandHandler(
                courtRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new CreateCourtCommand(
                Name: "Central Beach Court",
                Address: "Kyiv, Hydropark",
                Latitude: 50.4547,
                Longitude: 30.5861,
                SurfaceType: CourtSurfaceType.Sand,
                IsIndoor: false,
                Description: "Public beach volleyball court");

            // Act
            var courtId = await handler.Handle(command, CancellationToken.None);

            // Assert
            courtId.Should().NotBeEmpty();

            addedCourt.Should().NotBeNull();
            addedCourt!.Id.Should().Be(courtId);
            addedCourt.Name.Should().Be(command.Name);
            addedCourt.Address.Should().Be(command.Address);
            addedCourt.Latitude.Should().Be(command.Latitude);
            addedCourt.Longitude.Should().Be(command.Longitude);
            addedCourt.SurfaceType.Should().Be(command.SurfaceType);
            addedCourt.IsIndoor.Should().Be(command.IsIndoor);
            addedCourt.Description.Should().Be(command.Description);

            courtRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Court>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}