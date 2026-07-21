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
    public sealed class MvpOwnershipFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public MvpOwnershipFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task MvpFlow_ShouldApplyOwnershipRulesAcrossApi()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var anonymousClient = _factory.CreateClient();

            var createProfileWithoutTokenResponse = await anonymousClient.PostAsApiJsonAsync(
                "/api/player-profiles",
                new
                {
                    DisplayName = "Anonymous Player",
                    SkillLevel = PlayerSkillLevel.Intermediate,
                    City = "Amsterdam",
                    Bio = "Should not be created."
                });

            createProfileWithoutTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"organizer-{uniqueId}@test.com");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(
                playerClient,
                $"player-{uniqueId}@test.com");

            var organizerProfileId = await CreatePlayerProfileAsync(
                organizerClient,
                "Organizer Player");

            organizerProfileId.Should().NotBeEmpty();

            var playerProfileId = await CreatePlayerProfileAsync(
                playerClient,
                "Regular Player");

            playerProfileId.Should().NotBeEmpty();

            var courtId = await CreateCourtAsync(organizerClient);
            courtId.Should().NotBeEmpty();

            var createGameWithoutTokenResponse = await anonymousClient.PostAsApiJsonAsync(
                "/api/games",
                CreateGamePayload(courtId));

            createGameWithoutTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var gameId = await CreateGameAsync(organizerClient, courtId);
            gameId.Should().NotBeEmpty();

            var joinWithoutTokenResponse = await anonymousClient.PostAsync(
                $"/api/games/{gameId}/participants",
                content: null);

            joinWithoutTokenResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var participantId = await JoinGameAsync(playerClient, gameId);
            participantId.Should().NotBeEmpty();

            var approveByNonOrganizerResponse = await playerClient.PostAsync(
                $"/api/game-participants/{participantId}/approve",
                content: null);

            approveByNonOrganizerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var approveByOrganizerResponse = await organizerClient.PostAsync(
                $"/api/game-participants/{participantId}/approve",
                content: null);

            approveByOrganizerResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private static async Task RegisterAndAuthorizeAsync(
            HttpClient client,
            string email)
        {
            const string password = "Password123!";

            var registerResponse = await client.PostAsApiJsonAsync(
                "/api/auth/register",
                new
                {
                    Email = email,
                    Password = password
                });

            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var loginResponse = await client.PostAsApiJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password = password
                });

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResult = await loginResponse.Content.ReadFromApiJsonAsync<AuthResponse>();

            authResult.Should().NotBeNull();
            authResult!.AccessToken.Should().NotBeNullOrWhiteSpace();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
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

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateCourtAsync(HttpClient client)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/courts",
                new
                {
                    Name = "Integration Test Court",
                    Address = "Test Street 1",
                    Latitude = 52.3676,
                    Longitude = 4.9041,
                    SurfaceType = CourtSurfaceType.Indoor,
                    IsIndoor = true,
                    Description = "Court created by integration test."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateGameAsync(
            HttpClient client,
            Guid courtId)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/games",
                CreateGamePayload(courtId));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> JoinGameAsync(
            HttpClient client,
            Guid gameId)
        {
            var response = await client.PostAsync(
                $"/api/games/{gameId}/participants",
                content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static object CreateGamePayload(Guid courtId)
        {
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);

            return new
            {
                CourtId = courtId,
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(2),
                MaxPlayers = 12,
                PricePerPlayer = 15,
                RequiredLevel = GameLevel.Intermediate,
                JoinPolicy = GameJoinPolicy.ApprovalRequired,
                Description = "Integration test game."
            };
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);
    }
}