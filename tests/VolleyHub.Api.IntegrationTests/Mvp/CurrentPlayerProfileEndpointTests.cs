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

        [Fact]
        public async Task UpdateCurrentPlayerProfile_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            var client = _factory.CreateClient();

            var response = await client.PutAsJsonAsync(
                "/api/player-profiles/me",
                new
                {
                    DisplayName = "Updated Player",
                    SkillLevel = PlayerSkillLevel.Advanced,
                    City = "Rotterdam",
                    Bio = "Updated bio."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateCurrentPlayerProfile_ShouldReturnNotFound_WhenCurrentUserHasNoProfile()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var client = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                client,
                $"update-me-no-profile-{uniqueId}@test.com");

            var response = await client.PutAsJsonAsync(
                "/api/player-profiles/me",
                new
                {
                    DisplayName = "Updated Player",
                    SkillLevel = PlayerSkillLevel.Advanced,
                    City = "Rotterdam",
                    Bio = "Updated bio."
                });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateCurrentPlayerProfile_ShouldUpdateCurrentUserProfile_WhenProfileExists()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var client = _factory.CreateClient();

            var authResult = await RegisterAndAuthorizeAsync(
                client,
                $"update-me-profile-{uniqueId}@test.com");

            var profileId = await CreatePlayerProfileAsync(client);

            var updateResponse = await client.PutAsJsonAsync(
                "/api/player-profiles/me",
                new
                {
                    DisplayName = "Updated Current Player",
                    SkillLevel = PlayerSkillLevel.Advanced,
                    City = "Rotterdam",
                    Bio = "Updated current player profile."
                });

            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await client.GetAsync("/api/player-profiles/me");

            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var profile = await getResponse.Content.ReadFromJsonAsync<PlayerProfileResponse>();

            profile.Should().NotBeNull();
            profile!.Id.Should().Be(profileId);
            profile.UserId.Should().Be(authResult.UserId);
            profile.DisplayName.Should().Be("Updated Current Player");
            profile.SkillLevel.Should().Be(PlayerSkillLevel.Advanced);
            profile.City.Should().Be("Rotterdam");
            profile.Bio.Should().Be("Updated current player profile.");
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

        [Fact]
        public async Task CreatePlayerProfile_ShouldReturnConflict_WhenCurrentUserAlreadyHasProfile()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var client = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                client,
                $"duplicate-profile-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(client);

            var response = await client.PostAsJsonAsync(
                "/api/player-profiles",
                new
                {
                    DisplayName = "Duplicate Player",
                    SkillLevel = PlayerSkillLevel.Intermediate,
                    City = "Amsterdam",
                    Bio = "Duplicate profile."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            response.Content.Headers.ContentType?.MediaType.Should()
                .Be("application/problem+json");

            var problemDetails = await response.Content
                .ReadFromJsonAsync<ProblemDetailsResponse>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be((int)HttpStatusCode.Conflict);
            problemDetails.Title.Should().Be("Conflict");
            problemDetails.Detail.Should()
                .Be("User already has a player profile.");
            problemDetails.Instance.Should()
                .Be("/api/player-profiles");
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

        private sealed record ProblemDetailsResponse(
            int? Status,
            string? Title,
            string? Detail,
            string? Instance);
    }
}