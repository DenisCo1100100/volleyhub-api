using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Commands.UpdateCourt;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.UnitTests.Courts.Commands.UpdateCourt
{
    public sealed class UpdateCourtCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldUpdateCourtAndSaveChanges_WhenCourtExists()
        {
            // Arrange
            var court = CreateCourt();

            var courtRepositoryMock = new Mock<ICourtRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            Court? updatedCourt = null;

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(court);

            courtRepositoryMock
                .Setup(repository => repository.Update(It.IsAny<Court>()))
                .Callback<Court>(updated => updatedCourt = updated);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new UpdateCourtCommand(
                Id: court.Id,
                Name: "Updated Court",
                Address: "Updated Address",
                Latitude: 45,
                Longitude: 35,
                SurfaceType: CourtSurfaceType.Rubber,
                IsIndoor: true,
                Description: "Updated description");

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            updatedCourt.Should().NotBeNull();
            updatedCourt!.Id.Should().Be(court.Id);
            updatedCourt.Name.Should().Be(command.Name);
            updatedCourt.Address.Should().Be(command.Address);
            updatedCourt.Latitude.Should().Be(command.Latitude);
            updatedCourt.Longitude.Should().Be(command.Longitude);
            updatedCourt.SurfaceType.Should().Be(command.SurfaceType);
            updatedCourt.IsIndoor.Should().Be(command.IsIndoor);
            updatedCourt.Description.Should().Be(command.Description);

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            courtRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Court>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCourtDoesNotExist()
        {
            // Arrange
            var courtId = Guid.NewGuid();

            var courtRepositoryMock = new Mock<ICourtRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    courtId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Court?)null);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new UpdateCourtCommand(
                Id: courtId,
                Name: "Updated Court",
                Address: "Updated Address",
                Latitude: 45,
                Longitude: 35,
                SurfaceType: CourtSurfaceType.Rubber,
                IsIndoor: true,
                Description: "Updated description");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            courtRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<Court>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Court CreateCourt()
        {
            return Court.Create(
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