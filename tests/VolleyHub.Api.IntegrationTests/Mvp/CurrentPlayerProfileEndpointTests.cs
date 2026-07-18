using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class CurrentPlayerProfileEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CurrentPlayerProfileEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetCurrentPlayerProfile_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/player-profiles/me");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentPlayerProfile_ShouldReturnNotFound_WhenCurrentUserHasNoProfile()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var client = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                client,
                $"me-no-profile-{uniqueId}@test.com");

            var response = await client.GetAsync("/api/player-profiles/me");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetCurrentPlayerProfile_ShouldReturnCurrentUserProfile_WhenProfileExists()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var client = _factory.CreateClient();

            var authResult = await RegisterAndAuthorizeAsync(
                client,
                $"me-profile-{uniqueId}@test.com");

            var profileId = await CreatePlayerProfileAsync(client);

            var response = await client.GetAsync("/api/player-profiles/me");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var profile = await response.Content.ReadFromJsonAsync<PlayerProfileResponse>();

            profile.Should().NotBeNull();
            profile!.Id.Should().Be(profileId);
            profile.UserId.Should().Be(authResult.UserId);
            profile.DisplayName.Should().Be("Current Player");
            profile.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);
            profile.City.Should().Be("Amsterdam");
            profile.Bio.Should().Be("Current player profile.");
        }

        private static async Task<AuthResponse> RegisterAndAuthorizeAsync(
            HttpClient client,
            string email)
        {
            const string password = "Password123!";

            var registerResponse = await client.PostAsJsonAsync(
                "/api/auth/register",
                new
                {
                    Email = email,
                    Password = password
                });

            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var loginResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    Email = email,
                    Password = password
                });

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

            authResult.Should().NotBeNull();
            authResult!.AccessToken.Should().NotBeNullOrWhiteSpace();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                authResult.AccessToken);

            return authResult;
        }

        private static async Task<Guid> CreatePlayerProfileAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync(
                "/api/player-profiles",
                new
                {
                    DisplayName = "Current Player",
                    SkillLevel = PlayerSkillLevel.Intermediate,
                    City = "Amsterdam",
                    Bio = "Current player profile."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromJsonAsync<Guid>();
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);

        private sealed record PlayerProfileResponse(
            Guid Id,
            Guid UserId,
            string DisplayName,
            PlayerSkillLevel SkillLevel,
            string? City,
            string? Bio,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}