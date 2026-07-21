using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class MvpProfileRequiredTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public MvpProfileRequiredTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AuthenticatedUser_ShouldNeedPlayerProfile_ToCreateOrJoinGame()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var setupClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                setupClient,
                $"profile-required-setup-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                setupClient,
                $"Profile Required Setup {uniqueId}");

            var courtId = await CreateCourtAsync(
                setupClient);

            courtId.Should().NotBeEmpty();

            var clientWithoutProfile =
                _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                clientWithoutProfile,
                $"no-profile-{uniqueId}@test.com");

            var createGameResponse =
                await clientWithoutProfile.PostAsApiJsonAsync(
                    "/api/games",
                    CreateGamePayload(courtId));

            createGameResponse.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);

            var organizerClient =
                _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"profile-required-organizer-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                organizerClient,
                $"Profile Required Organizer {uniqueId}");

            var gameResponse =
                await organizerClient.PostAsApiJsonAsync(
                    "/api/games",
                    CreateGamePayload(courtId));

            gameResponse.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            var gameId = await gameResponse.Content
                .ReadFromApiJsonAsync<Guid>();

            var joinGameResponse =
                await clientWithoutProfile.PostAsync(
                    $"/api/games/{gameId}/participants",
                    content: null);

            joinGameResponse.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);
        }

        private static async Task RegisterAndAuthorizeAsync(
            HttpClient client,
            string email)
        {
            const string password = "Password123!";

            var registerResponse =
                await client.PostAsApiJsonAsync(
                    "/api/auth/register",
                    new
                    {
                        Email = email,
                        Password = password
                    });

            registerResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var loginResponse =
                await client.PostAsApiJsonAsync(
                    "/api/auth/login",
                    new
                    {
                        Email = email,
                        Password = password
                    });

            loginResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var authResult = await loginResponse.Content
                .ReadFromApiJsonAsync<AuthResponse>();

            authResult.Should().NotBeNull();

            authResult!.AccessToken.Should()
                .NotBeNullOrWhiteSpace();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    authResult.AccessToken);
        }

        private static async Task<Guid> CreatePlayerProfileAsync(
            HttpClient client,
            string displayName)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/player-profiles",
                new
                {
                    DisplayName = displayName,
                    SkillLevel = PlayerSkillLevel.Intermediate,
                    City = "Amsterdam",
                    Bio = "Integration test profile."
                });

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateCourtAsync(
            HttpClient client)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/courts",
                new
                {
                    Name =
                        $"Profile Required Test Court {Guid.NewGuid():N}",
                    Address = "Test Street 3",
                    Latitude = 52.3676,
                    Longitude = 4.9041,
                    SurfaceType = CourtSurfaceType.Indoor,
                    IsIndoor = true,
                    Description =
                        "Court created by profile required integration test."
                });

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static object CreateGamePayload(
            Guid courtId)
        {
            var startsAt =
                DateTimeOffset.UtcNow.AddDays(1);

            return new
            {
                CourtId = courtId,
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(2),
                MaxPlayers = 12,
                PricePerPlayer = 15,
                RequiredLevel = GameLevel.Intermediate,
                JoinPolicy =
                    GameJoinPolicy.ApprovalRequired,
                Description =
                    "Profile required integration test game."
            };
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);
    }
}