using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Commands.UpdatePlayerProfile
{
    public sealed class UpdatePlayerProfileCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldUpdatePlayerProfileAndSaveChanges_WhenCurrentUserOwnsProfile()
        {
            var playerProfile = CreatePlayerProfile();

            var command = new UpdatePlayerProfileCommand(
                Id: playerProfile.Id,
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(playerProfile.UserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            await handler.Handle(command, CancellationToken.None);

            playerProfile.DisplayName.Should().Be(command.DisplayName);
            playerProfile.SkillLevel.Should().Be(command.SkillLevel);
            playerProfile.City.Should().Be(command.City);
            playerProfile.Bio.Should().Be(command.Bio);

            playerProfileRepositoryMock.Verify(
                repository => repository.Update(playerProfile),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            playerProfileRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<PlayerProfile>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenCurrentUserDoesNotOwnProfile()
        {
            var playerProfile = CreatePlayerProfile();

            var command = new UpdatePlayerProfileCommand(
                Id: playerProfile.Id,
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(Guid.NewGuid());

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<PlayerProfile>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();

            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<PlayerProfile>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileIsDeleted()
        {
            var playerProfile = CreatePlayerProfile();
            playerProfile.Delete();

            var command = new UpdatePlayerProfileCommand(
                Id: playerProfile.Id,
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(playerProfile.UserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.Update(It.IsAny<PlayerProfile>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static PlayerProfile CreatePlayerProfile()
        {
            return PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "I like volleyball.");
        }
    }
}