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
    public sealed class MvpAttendanceFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public MvpAttendanceFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AttendanceFlow_ShouldAllowOnlyOrganizerToCompleteGameAndMarkAttendance()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"organizer-attendance-{uniqueId}@test.com");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(
                playerClient,
                $"player-attendance-{uniqueId}@test.com");

            var organizerProfileId = await CreatePlayerProfileAsync(
                organizerClient,
                "Attendance Organizer");

            organizerProfileId.Should().NotBeEmpty();

            var playerProfileId = await CreatePlayerProfileAsync(
                playerClient,
                "Attendance Player");

            playerProfileId.Should().NotBeEmpty();

            var courtId = await CreateCourtAsync();
            courtId.Should().NotBeEmpty();

            var gameId = await CreateGameAsync(organizerClient, courtId);
            gameId.Should().NotBeEmpty();

            var participantId = await JoinGameAsync(playerClient, gameId);
            participantId.Should().NotBeEmpty();

            var approveResponse = await organizerClient.PostAsync(
                $"/api/game-participants/{participantId}/approve",
                content: null);

            approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var completeByPlayerResponse = await playerClient.PostAsync(
                $"/api/games/{gameId}/complete",
                content: null);

            completeByPlayerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var completeByOrganizerResponse = await organizerClient.PostAsync(
                $"/api/games/{gameId}/complete",
                content: null);

            completeByOrganizerResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var markAttendanceByPlayerResponse = await playerClient.PostAsJsonAsync(
                $"/api/game-participants/{participantId}/attendance",
                new
                {
                    AttendanceStatus = GameParticipantAttendanceStatus.Present
                });

            markAttendanceByPlayerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var markAttendanceByOrganizerResponse = await organizerClient.PostAsJsonAsync(
                $"/api/game-participants/{participantId}/attendance",
                new
                {
                    AttendanceStatus = GameParticipantAttendanceStatus.Present
                });

            markAttendanceByOrganizerResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private static async Task RegisterAndAuthorizeAsync(
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
        }

        private static async Task<Guid> CreatePlayerProfileAsync(
            HttpClient client,
            string displayName)
        {
            var response = await client.PostAsJsonAsync(
                "/api/player-profiles",
                new
                {
                    DisplayName = displayName,
                    SkillLevel = PlayerSkillLevel.Intermediate,
                    City = "Amsterdam",
                    Bio = "Integration test profile."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromJsonAsync<Guid>();
        }

        private async Task<Guid> CreateCourtAsync()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/courts",
                new
                {
                    Name = "Attendance Test Court",
                    Address = "Test Street 2",
                    Latitude = 52.3676,
                    Longitude = 4.9041,
                    SurfaceType = CourtSurfaceType.Indoor,
                    IsIndoor = true,
                    Description = "Court created by attendance integration test."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateGameAsync(
            HttpClient client,
            Guid courtId)
        {
            var response = await client.PostAsJsonAsync(
                "/api/games",
                CreateGamePayload(courtId));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromJsonAsync<Guid>();
        }

        private static async Task<Guid> JoinGameAsync(
            HttpClient client,
            Guid gameId)
        {
            var response = await client.PostAsync(
                $"/api/games/{gameId}/participants",
                content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromJsonAsync<Guid>();
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
                Description = "Attendance integration test game."
            };
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);
    }
}