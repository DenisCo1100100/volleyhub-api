using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class GameSummaryEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GameSummaryEndpointTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetGames_ShouldReturnParticipantSummaryForAnonymousUser()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"game-summary-organizer-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                organizerClient,
                "Game Summary Organizer");

            var approvedPlayerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                approvedPlayerClient,
                $"game-summary-approved-player-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                approvedPlayerClient,
                "Approved Player");

            var pendingPlayerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                pendingPlayerClient,
                $"game-summary-pending-player-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                pendingPlayerClient,
                "Pending Player");

            var courtId = await CreateCourtAsync(organizerClient);

            var gameId = await CreateGameAsync(
                organizerClient,
                courtId);

            var approvedParticipantId = await JoinGameAsync(
                approvedPlayerClient,
                gameId);

            var approveResponse = await organizerClient.PostAsync(
                $"/api/game-participants/{approvedParticipantId}/approve",
                content: null);

            approveResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            await JoinGameAsync(
                pendingPlayerClient,
                gameId);

            var anonymousClient = _factory.CreateClient();

            var response = await anonymousClient.GetAsync(
                "/api/games");

            response.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var games = await response.Content
                .ReadFromApiJsonAsync<List<GameResponse>>();

            games.Should().NotBeNull();

            var game = games!.Single(item => item.Id == gameId);

            game.ApprovedParticipantCount.Should().Be(1);
            game.PendingParticipantCount.Should().Be(1);
            game.AvailableSpots.Should().Be(11);
            game.CurrentUserJoinStatus.Should().BeNull();
        }

        [Fact]
        public async Task GetGameById_ShouldReturnParticipantSummaryAndCurrentUserJoinStatus()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"game-summary-detail-organizer-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                organizerClient,
                "Game Summary Detail Organizer");

            var approvedPlayerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                approvedPlayerClient,
                $"game-summary-detail-approved-player-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                approvedPlayerClient,
                "Approved Detail Player");

            var pendingPlayerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                pendingPlayerClient,
                $"game-summary-detail-pending-player-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                pendingPlayerClient,
                "Pending Detail Player");

            var courtId = await CreateCourtAsync(organizerClient);

            var gameId = await CreateGameAsync(
                organizerClient,
                courtId);

            var approvedParticipantId = await JoinGameAsync(
                approvedPlayerClient,
                gameId);

            var approveResponse = await organizerClient.PostAsync(
                $"/api/game-participants/{approvedParticipantId}/approve",
                content: null);

            approveResponse.StatusCode.Should()
                .Be(HttpStatusCode.NoContent);

            await JoinGameAsync(
                pendingPlayerClient,
                gameId);

            var response = await pendingPlayerClient.GetAsync(
                $"/api/games/{gameId}");

            response.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var responseJson = await response.Content
                .ReadAsStringAsync();

            using var jsonDocument = JsonDocument.Parse(
                responseJson);

            var gameJson = jsonDocument.RootElement;

            AssertStringEnum(
                gameJson,
                "requiredLevel",
                nameof(GameLevel.Intermediate));

            AssertStringEnum(
                gameJson,
                "joinPolicy",
                nameof(GameJoinPolicy.ApprovalRequired));

            AssertStringEnum(
                gameJson,
                "status",
                nameof(GameStatus.Open));

            AssertStringEnum(
                gameJson,
                "currentUserJoinStatus",
                nameof(GameParticipantJoinStatus.PendingApproval));

            var game = await response.Content
                .ReadFromApiJsonAsync<GameResponse>();

            game.Should().NotBeNull();
            game!.Id.Should().Be(gameId);
            game.ApprovedParticipantCount.Should().Be(1);
            game.PendingParticipantCount.Should().Be(1);
            game.AvailableSpots.Should().Be(11);

            game.CurrentUserJoinStatus.Should()
                .Be(GameParticipantJoinStatus.PendingApproval);

            var participantsResponse = await pendingPlayerClient.GetAsync(
                $"/api/games/{gameId}/participants");

            participantsResponse.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var participantsJson = await participantsResponse.Content
                .ReadAsStringAsync();

            using var participantsDocument = JsonDocument.Parse(
                participantsJson);

            var participants = participantsDocument.RootElement;

            participants.GetArrayLength().Should()
                .BeGreaterThan(0);

            foreach (var participant in participants.EnumerateArray())
            {
                participant
                    .GetProperty("joinStatus")
                    .ValueKind.Should()
                    .Be(JsonValueKind.String);

                participant
                    .GetProperty("attendanceStatus")
                    .ValueKind.Should()
                    .Be(JsonValueKind.String);

                participant
                    .GetProperty("offlinePaymentStatus")
                    .ValueKind.Should()
                    .Be(JsonValueKind.String);
            }
        }

        private static void AssertStringEnum(
            JsonElement jsonElement,
            string propertyName,
            string expectedValue)
        {
            var property = jsonElement.GetProperty(propertyName);

            property.ValueKind.Should()
                .Be(JsonValueKind.String);

            property.GetString().Should()
                .Be(expectedValue);
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
                    Name = "Game Summary Test Court",
                    Address = "Test Street 20",
                    Latitude = 52.3676,
                    Longitude = 4.9041,
                    SurfaceType = CourtSurfaceType.Indoor,
                    IsIndoor = true,
                    Description =
                        "Court created by game summary integration test."
                });

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateGameAsync(
            HttpClient client,
            Guid courtId)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/games",
                CreateGamePayload(courtId));

            response.StatusCode.Should()
                .Be(HttpStatusCode.Created);

            return await response.Content
                .ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> JoinGameAsync(
            HttpClient client,
            Guid gameId)
        {
            var response = await client.PostAsync(
                $"/api/games/{gameId}/participants",
                content: null);

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
                Description = "Game summary integration test game."
            };
        }

        private sealed record AuthResponse(
            Guid UserId,
            string Email,
            string AccessToken);

        private sealed record GameResponse(
            Guid Id,
            Guid OrganizerId,
            Guid CourtId,
            DateTimeOffset StartsAt,
            DateTimeOffset? EndsAt,
            int MaxPlayers,
            decimal PricePerPlayer,
            GameLevel RequiredLevel,
            GameJoinPolicy JoinPolicy,
            string? Description,
            GameStatus Status,
            int ApprovedParticipantCount,
            int PendingParticipantCount,
            int AvailableSpots,
            GameParticipantJoinStatus? CurrentUserJoinStatus,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);
    }
}