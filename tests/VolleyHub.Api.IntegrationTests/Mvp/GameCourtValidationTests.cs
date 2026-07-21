using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class GameCourtValidationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GameCourtValidationTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateGame_ShouldReturnNotFound_WhenCourtDoesNotExist()
        {
            var uniqueId = Guid.NewGuid().ToString("N");
            var missingCourtId = Guid.NewGuid();

            var client = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                client,
                $"missing-game-court-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(client);

            var response = await client.PostAsApiJsonAsync(
                "/api/games",
                CreateGamePayload(missingCourtId));

            response.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);

            response.Content.Headers.ContentType?.MediaType.Should()
                .Be("application/problem+json");

            var problemDetails = await response.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problemDetails.Should().NotBeNull();

            problemDetails!.Status.Should()
                .Be((int)HttpStatusCode.NotFound);

            problemDetails.Detail.Should()
                .Be($"Court with key '{missingCourtId}' was not found.");

            problemDetails.Instance.Should()
                .Be("/api/games");
        }

        [Fact]
        public async Task CreateGame_ShouldReturnNotFound_WhenCourtIsDeleted()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var client = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                client,
                $"deleted-game-court-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(client);

            var courtId = await CreateCourtAsync(client);

            var deleteResponse = await client.DeleteAsync(
                $"/api/courts/{courtId}");

            deleteResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            var createGameResponse = await client.PostAsApiJsonAsync(
                "/api/games",
                CreateGamePayload(courtId));

            createGameResponse.StatusCode.Should()
                .Be(HttpStatusCode.NotFound);

            createGameResponse.Content.Headers.ContentType?.MediaType.Should()
                .Be("application/problem+json");

            var problemDetails = await createGameResponse.Content
                .ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problemDetails.Should().NotBeNull();

            problemDetails!.Status.Should()
                .Be((int)HttpStatusCode.NotFound);

            problemDetails.Detail.Should()
                .Be($"Court with key '{courtId}' was not found.");

            problemDetails.Instance.Should()
                .Be("/api/games");
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

            registerResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var loginResponse = await client.PostAsApiJsonAsync(
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
            HttpClient client)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/player-profiles",
                new
                {
                    DisplayName = "Game Court Validation Player",
                    SkillLevel = PlayerSkillLevel.Intermediate,
                    City = "Amsterdam",
                    Bio = "Game court validation integration test."
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
                    Name = $"Game Validation Court {Guid.NewGuid():N}",
                    Address = "Validation Street 1",
                    Latitude = 52.3676,
                    Longitude = 4.9041,
                    SurfaceType = CourtSurfaceType.Indoor,
                    IsIndoor = true,
                    Description = "Court used for game validation testing."
                });

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static object CreateGamePayload(
            Guid courtId)
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
                Description = "Game court validation integration test."
            };
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);

        private sealed record ProblemDetailsResponse(
            int? Status,
            string? Title,
            string? Detail,
            string? Instance);
    }
}