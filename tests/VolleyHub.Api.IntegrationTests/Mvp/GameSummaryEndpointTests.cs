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
        public async Task GetGames_ShouldReturnFrontendGameSummaryForAnonymousUser()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"game-summary-organizer-{uniqueId}@test.com");

            var organizerProfileId = await CreatePlayerProfileAsync(
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

            var courtId = await CreateCourtAsync(
                organizerClient);

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
                .ReadFromApiJsonAsync<List<GameSummaryResponse>>();

            games.Should().NotBeNull();

            var game = games!.Single(
                item => item.Id == gameId);

            game.Court.Id.Should().Be(courtId);
            game.Court.Name.Should().Be("Game Summary Test Court");
            game.Court.Address.Should().Be("Test Street 20");
            game.Court.Latitude.Should().Be(52.3676);
            game.Court.Longitude.Should().Be(4.9041);
            game.Court.SurfaceType.Should().Be(CourtSurfaceType.Indoor);
            game.Court.IsIndoor.Should().BeTrue();

            game.Organizer.Id.Should().Be(organizerProfileId);
            game.Organizer.DisplayName.Should().Be("Game Summary Organizer");
            game.Organizer.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);

            game.MaxPlayers.Should().Be(12);
            game.PricePerPlayer.Should().Be(15);
            game.RequiredLevel.Should().Be(GameLevel.Intermediate);
            game.JoinPolicy.Should().Be(GameJoinPolicy.ApprovalRequired);
            game.Status.Should().Be(GameStatus.Open);
            game.ApprovedParticipantCount.Should().Be(1);
            game.PendingParticipantCount.Should().Be(1);
            game.AvailableSpots.Should().Be(11);
            game.CurrentUserJoinStatus.Should().BeNull();
        }

        [Fact]
        public async Task GetGameById_ShouldReturnFrontendGameDetailsAndParticipants()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"game-details-organizer-{uniqueId}@test.com");

            var organizerProfileId = await CreatePlayerProfileAsync(
                organizerClient,
                "Game Details Organizer");

            var approvedPlayerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                approvedPlayerClient,
                $"game-details-approved-player-{uniqueId}@test.com");

            var approvedPlayerProfileId = await CreatePlayerProfileAsync(
                approvedPlayerClient,
                "Approved Detail Player");

            var pendingPlayerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                pendingPlayerClient,
                $"game-details-pending-player-{uniqueId}@test.com");

            var pendingPlayerProfileId = await CreatePlayerProfileAsync(
                pendingPlayerClient,
                "Pending Detail Player");

            var courtId = await CreateCourtAsync(
                organizerClient);

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

            var pendingParticipantId = await JoinGameAsync(
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

            var courtJson = gameJson.GetProperty("court");

            AssertStringEnum(
                courtJson,
                "surfaceType",
                nameof(CourtSurfaceType.Indoor));

            var organizerJson = gameJson.GetProperty("organizer");

            AssertStringEnum(
                organizerJson,
                "skillLevel",
                nameof(PlayerSkillLevel.Intermediate));

            foreach (var participantJson in gameJson
                         .GetProperty("participants")
                         .EnumerateArray())
            {
                AssertStringEnumProperty(
                    participantJson,
                    "skillLevel");

                AssertStringEnumProperty(
                    participantJson,
                    "joinStatus");

                AssertStringEnumProperty(
                    participantJson,
                    "attendanceStatus");

                AssertStringEnumProperty(
                    participantJson,
                    "offlinePaymentStatus");
            }

            var game = await response.Content
                .ReadFromApiJsonAsync<GameDetailsResponse>();

            game.Should().NotBeNull();
            game!.Id.Should().Be(gameId);

            game.Court.Id.Should().Be(courtId);
            game.Court.Name.Should().Be("Game Summary Test Court");
            game.Court.Address.Should().Be("Test Street 20");
            game.Court.SurfaceType.Should().Be(CourtSurfaceType.Indoor);
            game.Court.IsIndoor.Should().BeTrue();

            game.Organizer.Id.Should().Be(organizerProfileId);
            game.Organizer.DisplayName.Should().Be("Game Details Organizer");
            game.Organizer.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);

            game.Description.Should().Be("Game summary integration test game.");
            game.ApprovedParticipantCount.Should().Be(1);
            game.PendingParticipantCount.Should().Be(1);
            game.AvailableSpots.Should().Be(11);

            game.CurrentUserJoinStatus.Should()
                .Be(GameParticipantJoinStatus.PendingApproval);

            game.Participants.Should().HaveCount(2);

            var approvedParticipant = game.Participants.Single(
                participant => participant.Id == approvedParticipantId);

            approvedParticipant.PlayerProfileId.Should()
                .Be(approvedPlayerProfileId);

            approvedParticipant.DisplayName.Should()
                .Be("Approved Detail Player");

            approvedParticipant.SkillLevel.Should()
                .Be(PlayerSkillLevel.Intermediate);

            approvedParticipant.JoinStatus.Should()
                .Be(GameParticipantJoinStatus.Approved);

            var pendingParticipant = game.Participants.Single(
                participant => participant.Id == pendingParticipantId);

            pendingParticipant.PlayerProfileId.Should()
                .Be(pendingPlayerProfileId);

            pendingParticipant.DisplayName.Should()
                .Be("Pending Detail Player");

            pendingParticipant.SkillLevel.Should()
                .Be(PlayerSkillLevel.Intermediate);

            pendingParticipant.JoinStatus.Should()
                .Be(GameParticipantJoinStatus.PendingApproval);
        }

        [Fact]
        public async Task GetGameParticipants_ShouldReturnFrontendParticipantSummaries()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                organizerClient,
                $"participant-summary-organizer-{uniqueId}@test.com");

            await CreatePlayerProfileAsync(
                organizerClient,
                "Participant Summary Organizer");

            var playerClient = _factory.CreateClient();

            await RegisterAndAuthorizeAsync(
                playerClient,
                $"participant-summary-player-{uniqueId}@test.com");

            var playerProfileId = await CreatePlayerProfileAsync(
                playerClient,
                "Participant Summary Player");

            var courtId = await CreateCourtAsync(
                organizerClient);

            var gameId = await CreateGameAsync(
                organizerClient,
                courtId);

            var participantId = await JoinGameAsync(
                playerClient,
                gameId);

            var response = await _factory
                .CreateClient()
                .GetAsync($"/api/games/{gameId}/participants");

            response.StatusCode.Should()
                .Be(HttpStatusCode.OK);

            var participants = await response.Content
                .ReadFromApiJsonAsync<List<GameParticipantSummaryResponse>>();

            participants.Should().NotBeNull();
            participants.Should().ContainSingle();

            var participant = participants!.Single();

            participant.Id.Should().Be(participantId);
            participant.PlayerProfileId.Should().Be(playerProfileId);
            participant.DisplayName.Should().Be("Participant Summary Player");
            participant.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);

            participant.JoinStatus.Should()
                .Be(GameParticipantJoinStatus.PendingApproval);

            participant.AttendanceStatus.Should()
                .Be(GameParticipantAttendanceStatus.NotMarked);

            participant.OfflinePaymentStatus.Should()
                .Be(GameParticipantOfflinePaymentStatus.Pending);
        }

        private static void AssertStringEnum(
            JsonElement jsonElement,
            string propertyName,
            string expectedValue)
        {
            var property = jsonElement.GetProperty(
                propertyName);

            property.ValueKind.Should()
                .Be(JsonValueKind.String);

            property.GetString().Should()
                .Be(expectedValue);
        }

        private static void AssertStringEnumProperty(
            JsonElement jsonElement,
            string propertyName)
        {
            jsonElement
                .GetProperty(propertyName)
                .ValueKind.Should()
                .Be(JsonValueKind.String);
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

        private sealed record GameCourtResponse(
            Guid Id,
            string Name,
            string Address,
            double Latitude,
            double Longitude,
            CourtSurfaceType SurfaceType,
            bool IsIndoor);

        private sealed record GameOrganizerResponse(
            Guid Id,
            string DisplayName,
            PlayerSkillLevel SkillLevel);

        private sealed record GameSummaryResponse(
            Guid Id,
            GameCourtResponse Court,
            GameOrganizerResponse Organizer,
            DateTimeOffset StartsAt,
            DateTimeOffset? EndsAt,
            int MaxPlayers,
            decimal PricePerPlayer,
            GameLevel RequiredLevel,
            GameJoinPolicy JoinPolicy,
            GameStatus Status,
            int ApprovedParticipantCount,
            int PendingParticipantCount,
            int AvailableSpots,
            GameParticipantJoinStatus? CurrentUserJoinStatus);

        private sealed record GameDetailsResponse(
            Guid Id,
            GameCourtResponse Court,
            GameOrganizerResponse Organizer,
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
            IReadOnlyList<GameParticipantSummaryResponse> Participants,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt);

        private sealed record GameParticipantSummaryResponse(
            Guid Id,
            Guid PlayerProfileId,
            string DisplayName,
            PlayerSkillLevel SkillLevel,
            GameParticipantJoinStatus JoinStatus,
            GameParticipantAttendanceStatus AttendanceStatus,
            GameParticipantOfflinePaymentStatus OfflinePaymentStatus,
            DateTimeOffset JoinedAt,
            DateTimeOffset? ApprovedAt);
    }
}