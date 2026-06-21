using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Commands.DeleteCourt;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.UnitTests.Courts.Commands.DeleteCourt
{
    public sealed class DeleteCourtCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldDeleteCourtAndSaveChanges_WhenCourtExists()
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

            var handler = new DeleteCourtCommandHandler(
                courtRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new DeleteCourtCommand(court.Id);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            updatedCourt.Should().NotBeNull();
            updatedCourt!.Id.Should().Be(court.Id);
            updatedCourt.IsDeleted.Should().BeTrue();

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

            var handler = new DeleteCourtCommandHandler(
                courtRepositoryMock.Object,
                unitOfWorkMock.Object);

            var command = new DeleteCourtCommand(courtId);

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