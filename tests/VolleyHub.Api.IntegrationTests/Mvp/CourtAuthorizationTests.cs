using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class CourtAuthorizationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CourtAuthorizationTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CourtWriteEndpoints_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            var client = _factory.CreateClient();
            var courtId = Guid.NewGuid();

            var createResponse = await client.PostAsApiJsonAsync(
                "/api/courts",
                CreateCourtPayload(
                    "Anonymous Create Court"));

            var updateResponse = await client.PutAsApiJsonAsync(
                $"/api/courts/{courtId}",
                UpdateCourtPayload(
                    courtId,
                    "Anonymous Update Court"));

            var deleteResponse = await client.DeleteAsync(
                $"/api/courts/{courtId}");

            createResponse.StatusCode.Should()
                .Be(HttpStatusCode.Unauthorized);

            updateResponse.StatusCode.Should()
                .Be(HttpStatusCode.Unauthorized);

            deleteResponse.StatusCode.Should()
                .Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CourtWriteEndpoints_ShouldAllowOwner()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var ownerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                ownerClient,
                $"court-owner-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                ownerClient,
                $"Court Owner {uniqueId}");

            var courtId = await CreateCourtAsync(
                ownerClient,
                $"Owner Court {uniqueId}");

            var updateResponse =
                await ownerClient.PutAsApiJsonAsync(
                    $"/api/courts/{courtId}",
                    UpdateCourtPayload(
                        courtId,
                        $"Updated Owner Court {uniqueId}"));

            updateResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            var deleteResponse = await ownerClient.DeleteAsync(
                $"/api/courts/{courtId}");

            deleteResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task CourtWriteEndpoints_ShouldReturnForbidden_WhenCurrentUserDoesNotOwnCourt()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var ownerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                ownerClient,
                $"court-owner-forbidden-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                ownerClient,
                $"Court Owner Forbidden {uniqueId}");

            var courtId = await CreateCourtAsync(
                ownerClient,
                $"Protected Court {uniqueId}");

            var otherUserClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                otherUserClient,
                $"court-other-user-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                otherUserClient,
                $"Other Court User {uniqueId}");

            var updateResponse =
                await otherUserClient.PutAsApiJsonAsync(
                    $"/api/courts/{courtId}",
                    UpdateCourtPayload(
                        courtId,
                        $"Unauthorized Update {uniqueId}"));

            updateResponse.StatusCode.Should()
                .Be(HttpStatusCode.Forbidden);

            updateResponse.Content.Headers.ContentType?.MediaType
                .Should()
                .Be("application/problem+json");

            var deleteResponse = await otherUserClient.DeleteAsync(
                $"/api/courts/{courtId}");

            deleteResponse.StatusCode.Should()
                .Be(HttpStatusCode.Forbidden);

            deleteResponse.Content.Headers.ContentType?.MediaType
                .Should()
                .Be("application/problem+json");

            var getCourtResponse = await ownerClient.GetAsync(
                $"/api/courts/{courtId}");

            getCourtResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CourtReadEndpoints_ShouldStayPublic()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var ownerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                ownerClient,
                $"court-reader-setup-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                ownerClient,
                $"Court Reader Setup {uniqueId}");

            var courtId = await CreateCourtAsync(
                ownerClient,
                $"Public Court {uniqueId}");

            var anonymousClient = _factory.CreateClient();

            var getCourtsResponse =
                await anonymousClient.GetAsync(
                    "/api/courts");

            var getCourtByIdResponse =
                await anonymousClient.GetAsync(
                    $"/api/courts/{courtId}");

            getCourtsResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            getCourtByIdResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);
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
                    Bio = "Court ownership integration test profile."
                });

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateCourtAsync(
            HttpClient client,
            string name)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/courts",
                CreateCourtPayload(name));

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static object CreateCourtPayload(
            string name)
        {
            return new
            {
                Name = name,
                Address = "Test Street 10",
                Latitude = 52.3676,
                Longitude = 4.9041,
                SurfaceType = CourtSurfaceType.Indoor,
                IsIndoor = true,
                Description =
                    "Court authorization integration test."
            };
        }

        private static object UpdateCourtPayload(
            Guid courtId,
            string name)
        {
            return new
            {
                Id = courtId,
                Name = name,
                Address = "Updated Test Street 10",
                Latitude = 52.3677,
                Longitude = 4.9042,
                SurfaceType = CourtSurfaceType.Indoor,
                IsIndoor = true,
                Description =
                    "Updated court authorization integration test."
            };
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);
    }
}