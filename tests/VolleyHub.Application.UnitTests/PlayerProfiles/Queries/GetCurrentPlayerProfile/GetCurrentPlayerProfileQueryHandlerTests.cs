using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Queries.GetCurrentPlayerProfile;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetCurrentPlayerProfile
{
    public sealed class GetCurrentPlayerProfileQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnCurrentPlayerProfile_WhenProfileExists()
        {
            var currentUserId = Guid.NewGuid();

            var playerProfile = PlayerProfile.Create(
                currentUserId,
                "John Player",
                PlayerSkillLevel.Intermediate,
                "Amsterdam",
                "I like volleyball.");

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var currentUserServiceMock = new Mock<ICurrentUserService>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            var handler = new GetCurrentPlayerProfileQueryHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object);

            var query = new GetCurrentPlayerProfileQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            result.Id.Should().Be(playerProfile.Id);
            result.UserId.Should().Be(playerProfile.UserId);
            result.DisplayName.Should().Be(playerProfile.DisplayName);
            result.SkillLevel.Should().Be(playerProfile.SkillLevel);
            result.City.Should().Be(playerProfile.City);
            result.Bio.Should().Be(playerProfile.Bio);
            result.CreatedAt.Should().Be(playerProfile.CreatedAt);
            result.UpdatedAt.Should().Be(playerProfile.UpdatedAt);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedException_WhenCurrentUserDoesNotExist()
        {
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            var currentUserServiceMock = new Mock<ICurrentUserService>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns((Guid?)null);

            var handler = new GetCurrentPlayerProfileQueryHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object);

            var query = new GetCurrentPlayerProfileQuery();

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();

            playerProfileRepositoryMock.Verify(
                repository => repository.GetByUserIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileDoesNotExist()
        {
            var currentUserId = Guid.NewGuid();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var currentUserServiceMock = new Mock<ICurrentUserService>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            var handler = new GetCurrentPlayerProfileQueryHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object);

            var query = new GetCurrentPlayerProfileQuery();

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileIsDeleted()
        {
            var currentUserId = Guid.NewGuid();

            var playerProfile = PlayerProfile.Create(
                currentUserId,
                "John Player",
                PlayerSkillLevel.Intermediate,
                "Amsterdam",
                "I like volleyball.");

            playerProfile.Delete();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByUserIdAsync(
                    currentUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var currentUserServiceMock = new Mock<ICurrentUserService>();

            currentUserServiceMock
                .Setup(service => service.UserId)
                .Returns(currentUserId);

            var handler = new GetCurrentPlayerProfileQueryHandler(
                playerProfileRepositoryMock.Object,
                currentUserServiceMock.Object);

            var query = new GetCurrentPlayerProfileQuery();

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}