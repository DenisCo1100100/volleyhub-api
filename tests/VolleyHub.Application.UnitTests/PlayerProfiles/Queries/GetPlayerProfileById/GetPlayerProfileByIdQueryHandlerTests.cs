using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetPlayerProfileById
{
    public sealed class GetPlayerProfileByIdQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnPlayerProfile_WhenProfileExists()
        {
            var playerProfile = CreatePlayerProfile();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new GetPlayerProfileByIdQueryHandler(
                playerProfileRepositoryMock.Object);

            var query = new GetPlayerProfileByIdQuery(playerProfile.Id);

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
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileDoesNotExist()
        {
            var profileId = Guid.NewGuid();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    profileId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((PlayerProfile?)null);

            var handler = new GetPlayerProfileByIdQueryHandler(
                playerProfileRepositoryMock.Object);

            var query = new GetPlayerProfileByIdQuery(profileId);

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProfileIsDeleted()
        {
            var playerProfile = CreatePlayerProfile();
            playerProfile.Delete();

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetByIdAsync(
                    playerProfile.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfile);

            var handler = new GetPlayerProfileByIdQueryHandler(
                playerProfileRepositoryMock.Object);

            var query = new GetPlayerProfileByIdQuery(playerProfile.Id);

            Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
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