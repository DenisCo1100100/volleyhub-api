using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Commands.UpdateCourt;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Courts.Commands.UpdateCourt
{
    public sealed class UpdateCourtCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldUpdateCourtAndSaveChanges_WhenCurrentUserOwnsCourt()
        {
            var ownerProfile = CreatePlayerProfile();
            var court = CreateCourt(ownerProfile.Id);
            var command = CreateValidCommand(court.Id);

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(ownerProfile.UserId);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(court);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    ownerProfile.UserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ownerProfile);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            await handler.Handle(
                command,
                CancellationToken.None);

            court.OwnerPlayerProfileId.Should()
                .Be(ownerProfile.Id);

            court.Name.Should()
                .Be(command.Name);

            court.Address.Should()
                .Be(command.Address);

            court.Latitude.Should()
                .Be(command.Latitude);

            court.Longitude.Should()
                .Be(command.Longitude);

            court.SurfaceType.Should()
                .Be(command.SurfaceType);

            court.IsIndoor.Should()
                .Be(command.IsIndoor);

            court.Description.Should()
                .Be(command.Description);

            courtRepositoryMock.Verify(
                repository => repository.Update(court),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var command = CreateValidCommand(
                Guid.NewGuid());

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<UnauthorizedException>();

            courtRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            courtRepositoryMock.Verify(
                repository => repository.Update(
                    It.IsAny<Court>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCourtDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();
            var command = CreateValidCommand(
                Guid.NewGuid());

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Court?)null);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            courtRepositoryMock.Verify(
                repository => repository.Update(
                    It.IsAny<Court>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserDoesNotHavePlayerProfile()
        {
            var currentUserId = Guid.NewGuid();
            var court = CreateCourt(Guid.NewGuid());
            var command = CreateValidCommand(court.Id);

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(court);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            courtRepositoryMock.Verify(
                repository => repository.Update(
                    It.IsAny<Court>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentPlayerProfile =
                CreatePlayerProfile();

            currentPlayerProfile.Delete();

            var court = CreateCourt(
                currentPlayerProfile.Id);

            var command = CreateValidCommand(
                court.Id);

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentPlayerProfile.UserId);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(court);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentPlayerProfile.UserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentPlayerProfile);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            courtRepositoryMock.Verify(
                repository => repository.Update(
                    It.IsAny<Court>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenCurrentUserDoesNotOwnCourt()
        {
            var ownerProfile = CreatePlayerProfile();
            var currentPlayerProfile = CreatePlayerProfile();

            var court = CreateCourt(
                ownerProfile.Id);

            var command = CreateValidCommand(
                court.Id);

            var originalName = court.Name;

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentPlayerProfile.UserId);

            courtRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    court.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(court);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentPlayerProfile.UserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentPlayerProfile);

            var handler = new UpdateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ForbiddenAccessException>();

            court.Name.Should()
                .Be(originalName);

            courtRepositoryMock.Verify(
                repository => repository.Update(
                    It.IsAny<Court>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static UpdateCourtCommand CreateValidCommand(
            Guid courtId)
        {
            return new UpdateCourtCommand(
                Id: courtId,
                Name: "Updated Court",
                Address: "Updated Address",
                Latitude: 45,
                Longitude: 35,
                SurfaceType: CourtSurfaceType.Rubber,
                IsIndoor: true,
                Description: "Updated description");
        }

        private static PlayerProfile CreatePlayerProfile()
        {
            return PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "Court User",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "Community court user.");
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