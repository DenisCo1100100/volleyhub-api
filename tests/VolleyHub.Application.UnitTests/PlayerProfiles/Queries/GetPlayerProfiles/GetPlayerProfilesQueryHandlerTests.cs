using FluentAssertions;
using Moq;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfiles;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.UnitTests.PlayerProfiles.Queries.GetPlayerProfiles
{
    public sealed class GetPlayerProfilesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnPlayerProfiles_WhenProfilesExist()
        {
            var firstProfile = CreatePlayerProfile(
                displayName: "John Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: "I like volleyball.");

            var secondProfile = CreatePlayerProfile(
                displayName: "Jane Player",
                skillLevel: PlayerSkillLevel.Advanced,
                city: "Rotterdam",
                bio: null);

            var playerProfiles = new List<PlayerProfile>
            {
                firstProfile,
                secondProfile
            };

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfiles);

            var handler = new GetPlayerProfilesQueryHandler(
                playerProfileRepositoryMock.Object);

            var query = new GetPlayerProfilesQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().HaveCount(2);

            result[0].Id.Should().Be(firstProfile.Id);
            result[0].UserId.Should().Be(firstProfile.UserId);
            result[0].DisplayName.Should().Be(firstProfile.DisplayName);
            result[0].SkillLevel.Should().Be(firstProfile.SkillLevel);
            result[0].City.Should().Be(firstProfile.City);
            result[0].Bio.Should().Be(firstProfile.Bio);

            result[1].Id.Should().Be(secondProfile.Id);
            result[1].UserId.Should().Be(secondProfile.UserId);
            result[1].DisplayName.Should().Be(secondProfile.DisplayName);
            result[1].SkillLevel.Should().Be(secondProfile.SkillLevel);
            result[1].City.Should().Be(secondProfile.City);
            result[1].Bio.Should().Be(secondProfile.Bio);
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyNotDeletedProfiles_WhenSomeProfilesAreDeleted()
        {
            var activeProfile = CreatePlayerProfile(
                displayName: "Active Player",
                skillLevel: PlayerSkillLevel.Intermediate,
                city: "Amsterdam",
                bio: null);

            var deletedProfile = CreatePlayerProfile(
                displayName: "Deleted Player",
                skillLevel: PlayerSkillLevel.Beginner,
                city: "Utrecht",
                bio: null);

            deletedProfile.Delete();

            var playerProfiles = new List<PlayerProfile>
            {
                activeProfile,
                deletedProfile
            };

            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(playerProfiles);

            var handler = new GetPlayerProfilesQueryHandler(
                playerProfileRepositoryMock.Object);

            var query = new GetPlayerProfilesQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().ContainSingle();
            result[0].Id.Should().Be(activeProfile.Id);
            result[0].DisplayName.Should().Be(activeProfile.DisplayName);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenProfilesDoNotExist()
        {
            var playerProfileRepositoryMock = new Mock<IPlayerProfileRepository>();

            playerProfileRepositoryMock
                .Setup(repository => repository.GetListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PlayerProfile>());

            var handler = new GetPlayerProfilesQueryHandler(
                playerProfileRepositoryMock.Object);

            var query = new GetPlayerProfilesQuery();

            var result = await handler.Handle(query, CancellationToken.None);

            result.Should().BeEmpty();
        }

        private static PlayerProfile CreatePlayerProfile(
            string displayName,
            PlayerSkillLevel skillLevel,
            string? city,
            string? bio)
        {
            return PlayerProfile.Create(
                userId: Guid.NewGuid(),
                displayName: displayName,
                skillLevel: skillLevel,
                city: city,
                bio: bio);
        }
    }
}