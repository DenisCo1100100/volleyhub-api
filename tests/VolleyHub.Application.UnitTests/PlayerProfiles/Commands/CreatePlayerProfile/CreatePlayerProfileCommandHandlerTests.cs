using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Commands.CreatePlayerProfile;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Commands.CreatePlayerProfile
{
    public sealed class CreatePlayerProfileCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCreatePlayerProfileAndSaveChanges_WhenCurrentUserDoesNotHaveProfile()
        {
            var currentUserId = Guid.NewGuid();

            var command = new CreatePlayerProfileCommand(
                DisplayName: "John Player",
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new CreatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            var playerProfileId = await handler.Handle(command, CancellationToken.None);

            playerProfileId.Should().NotBeEmpty();

            playerProfileRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<PlayerProfile>(playerProfile =>
                        playerProfile.Id == playerProfileId
                        && playerProfile.UserId == currentUserId
                        && playerProfile.DisplayName == command.DisplayName
                        && playerProfile.SkillLevel == command.SkillLevel
                        && playerProfile.City == command.City
                        && playerProfile.Bio == command.Bio
                        && !playerProfile.IsDeleted),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var command = new CreatePlayerProfileCommand(
                DisplayName: "John Player",
                SkillLevel: PlayerSkillLevel.Intermediate,
                City: "Amsterdam",
                Bio: "I like volleyball.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new CreatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            playerProfileRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<PlayerProfile>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessRuleException_WhenCurrentUserAlreadyHasProfile()
        {
            var existingProfile = CreatePlayerProfile();

            var command = new CreatePlayerProfileCommand(
                DisplayName: "Another Name",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: null);

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(existingProfile.UserId);

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    existingProfile.UserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingProfile);

            var handler = new CreatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object,
                unitOfWorkMock.Object);

            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<PlayerProfile>(),
                    It.IsAny<CancellationToken>()),
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