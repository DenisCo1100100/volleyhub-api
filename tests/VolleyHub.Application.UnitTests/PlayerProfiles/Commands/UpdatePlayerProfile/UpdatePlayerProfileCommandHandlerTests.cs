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
        public async Task Handle_ShouldUpdatePlayerProfileAndSaveChanges_WhenProfileExists()
        {
            var playerProfile = CreatePlayerProfile();

            var command = new UpdatePlayerProfileCommand(
                Id: playerProfile.Id,
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

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
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileDoesNotExist()
        {
            var command = new UpdatePlayerProfileCommand(
                Id: Guid.NewGuid(),
                DisplayName: "Updated Player",
                SkillLevel: PlayerSkillLevel.Advanced,
                City: "Rotterdam",
                Bio: "Updated bio.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
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
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    command.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new UpdatePlayerProfileCommandHandler(
                playerProfileRepositoryMock.Object,
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