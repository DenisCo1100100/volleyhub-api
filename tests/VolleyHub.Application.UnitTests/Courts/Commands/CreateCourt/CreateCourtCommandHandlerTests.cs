using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Commands.CreateCourt;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.Courts.Commands.CreateCourt
{
    public sealed class CreateCourtCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldAddCourtAndSaveChanges_WhenCurrentUserHasPlayerProfile()
        {
            var currentUserId = Guid.NewGuid();

            var ownerProfile = CreatePlayerProfile(
                currentUserId);

            var command = CreateValidCommand();

            var courtRepositoryMock =
                new Mock<ICourtRepository>();

            var playerProfileRepositoryMock =
                new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock =
                new Mock<ICurrentUserService>();

            var unitOfWorkMock =
                new Mock<IUnitOfWork>();

            Court? addedCourt = null;

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ownerProfile);

            courtRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Court>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Court, CancellationToken>(
                    (court, _) => addedCourt = court)
                .Returns(Task.CompletedTask);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new CreateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var courtId = await handler.Handle(
                command,
                CancellationToken.None);

            courtId.Should().NotBeEmpty();

            addedCourt.Should().NotBeNull();

            addedCourt!.Id.Should()
                .Be(courtId);

            addedCourt.OwnerPlayerProfileId.Should()
                .Be(ownerProfile.Id);

            addedCourt.Name.Should()
                .Be(command.Name);

            addedCourt.Address.Should()
                .Be(command.Address);

            addedCourt.Latitude.Should()
                .Be(command.Latitude);

            addedCourt.Longitude.Should()
                .Be(command.Longitude);

            addedCourt.SurfaceType.Should()
                .Be(command.SurfaceType);

            addedCourt.IsIndoor.Should()
                .Be(command.IsIndoor);

            addedCourt.Description.Should()
                .Be(command.Description);

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

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

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
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

            var handler = new CreateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand();

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            courtRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Court>(),
                    It.IsAny<CancellationToken>()),
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

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new CreateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand();

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            courtRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Court>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenCurrentUserPlayerProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();

            var ownerProfile = CreatePlayerProfile(
                currentUserId);

            ownerProfile.Delete();

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

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ownerProfile);

            var handler = new CreateCourtCommandHandler(
                courtRepositoryMock.Object,
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var command = CreateValidCommand();

            Func<Task> act = async () =>
                await handler.Handle(
                    command,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            courtRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Court>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static CreateCourtCommand CreateValidCommand()
        {
            return new CreateCourtCommand(
                Name: "Central Beach Court",
                Address: "Kyiv, Hydropark",
                Latitude: 50.4547,
                Longitude: 30.5861,
                SurfaceType: CourtSurfaceType.Sand,
                IsIndoor: false,
                Description: "Public beach volleyball court");
        }

        private static PlayerProfile CreatePlayerProfile(
            Guid userId)
        {
            return PlayerProfile.Create(
                userId: userId,
                displayName: "Court Owner",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "Community court owner.");
        }
    }
}