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
    public sealed class GameSummaryEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GameSummaryEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetGames_ShouldReturnPagedFrontendGameSummaryAndCurrentUserState()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"game-summary-organizer-{uniqueId}@test.com");
            var organizerProfileId = await CreatePlayerProfileAsync(organizerClient, "Game Summary Organizer");

            var approvedPlayerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(approvedPlayerClient, $"game-summary-approved-player-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(approvedPlayerClient, "Approved Player");

            var pendingPlayerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(pendingPlayerClient, $"game-summary-pending-player-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(pendingPlayerClient, "Pending Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Game Summary Court {uniqueId}");
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var gameId = await CreateGameAsync(organizerClient, courtId, startsAt);

            var approvedParticipantId = await JoinGameAsync(approvedPlayerClient, gameId);

            var approveResponse = await organizerClient.PostAsync($"/api/game-participants/{approvedParticipantId}/approve", content: null);
            approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await JoinGameAsync(pendingPlayerClient, gameId);

            var anonymousClient = _factory.CreateClient();
            var anonymousResponse = await anonymousClient.GetAsync($"/api/games?courtId={courtId}");

            anonymousResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var anonymousResult = await anonymousResponse.Content.ReadFromApiJsonAsync<PagedResponse<GameSummaryResponse>>();

            anonymousResult.Should().NotBeNull();
            anonymousResult!.Page.Should().Be(1);
            anonymousResult.PageSize.Should().Be(20);
            anonymousResult.TotalCount.Should().Be(1);
            anonymousResult.TotalPages.Should().Be(1);
            anonymousResult.Items.Should().ContainSingle();

            var anonymousGame = anonymousResult.Items.Single();

            anonymousGame.Id.Should().Be(gameId);
            anonymousGame.Court.Id.Should().Be(courtId);
            anonymousGame.Court.Name.Should().Be($"Game Summary Court {uniqueId}");
            anonymousGame.Court.Address.Should().Be("Test Street 20");
            anonymousGame.Court.Latitude.Should().Be(52.3676);
            anonymousGame.Court.Longitude.Should().Be(4.9041);
            anonymousGame.Court.SurfaceType.Should().Be(CourtSurfaceType.Indoor);
            anonymousGame.Court.IsIndoor.Should().BeTrue();

            anonymousGame.Organizer.Id.Should().Be(organizerProfileId);
            anonymousGame.Organizer.DisplayName.Should().Be("Game Summary Organizer");
            anonymousGame.Organizer.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);

            anonymousGame.StartsAt.Should().Be(startsAt);
            anonymousGame.MaxPlayers.Should().Be(12);
            anonymousGame.PricePerPlayer.Should().Be(15);
            anonymousGame.RequiredLevel.Should().Be(GameLevel.Intermediate);
            anonymousGame.JoinPolicy.Should().Be(GameJoinPolicy.ApprovalRequired);
            anonymousGame.Status.Should().Be(GameStatus.Open);
            anonymousGame.ApprovedParticipantCount.Should().Be(1);
            anonymousGame.PendingParticipantCount.Should().Be(1);
            anonymousGame.AvailableSpots.Should().Be(11);
            anonymousGame.CurrentUserJoinStatus.Should().BeNull();

            var authenticatedResponse = await pendingPlayerClient.GetAsync($"/api/games?courtId={courtId}");

            authenticatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var authenticatedResult = await authenticatedResponse.Content.ReadFromApiJsonAsync<PagedResponse<GameSummaryResponse>>();

            authenticatedResult.Should().NotBeNull();
            authenticatedResult!.Items.Should().ContainSingle();

            var authenticatedGame = authenticatedResult.Items.Single();

            authenticatedGame.Id.Should().Be(gameId);
            authenticatedGame.CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
        }

        [Fact]
        public async Task GetGames_ShouldApplyPaginationAndFiltersWithStableOrdering()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"game-filter-organizer-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(organizerClient, "Game Filter Organizer");

            var firstCourtId = await CreateCourtAsync(organizerClient, $"Filter Court A {uniqueId}");
            var secondCourtId = await CreateCourtAsync(organizerClient, $"Filter Court B {uniqueId}");

            var baseStartsAt = DateTimeOffset.UtcNow.AddDays(10);
            var beforeRangeStartsAt = baseStartsAt;
            var firstStartsAt = baseStartsAt.AddHours(1);
            var secondStartsAt = baseStartsAt.AddHours(2);
            var cancelledStartsAt = baseStartsAt.AddHours(3);

            await CreateGameAsync(organizerClient, firstCourtId, beforeRangeStartsAt);
            var firstGameId = await CreateGameAsync(organizerClient, firstCourtId, firstStartsAt);
            var secondGameId = await CreateGameAsync(organizerClient, firstCourtId, secondStartsAt);
            var cancelledGameId = await CreateGameAsync(organizerClient, firstCourtId, cancelledStartsAt);
            await CreateGameAsync(organizerClient, secondCourtId, secondStartsAt);

            var cancelResponse = await organizerClient.PostAsync($"/api/games/{cancelledGameId}/cancel", content: null);
            cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var startsAtFrom = Uri.EscapeDataString(firstStartsAt.ToString("O"));
            var startsAtTo = Uri.EscapeDataString(cancelledStartsAt.ToString("O"));

            var firstPageResponse = await _factory.CreateClient().GetAsync(
                $"/api/games?page=1&pageSize=1&courtId={firstCourtId}&status=Open&startsAtFrom={startsAtFrom}&startsAtTo={startsAtTo}");

            firstPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var firstPage = await firstPageResponse.Content.ReadFromApiJsonAsync<PagedResponse<GameSummaryResponse>>();

            firstPage.Should().NotBeNull();
            firstPage!.Page.Should().Be(1);
            firstPage.PageSize.Should().Be(1);
            firstPage.TotalCount.Should().Be(2);
            firstPage.TotalPages.Should().Be(2);
            firstPage.Items.Should().ContainSingle();
            firstPage.Items.Single().Id.Should().Be(firstGameId);
            firstPage.Items.Single().StartsAt.Should().Be(firstStartsAt);

            var secondPageResponse = await _factory.CreateClient().GetAsync(
                $"/api/games?page=2&pageSize=1&courtId={firstCourtId}&status=Open&startsAtFrom={startsAtFrom}&startsAtTo={startsAtTo}");

            secondPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondPage = await secondPageResponse.Content.ReadFromApiJsonAsync<PagedResponse<GameSummaryResponse>>();

            secondPage.Should().NotBeNull();
            secondPage!.Page.Should().Be(2);
            secondPage.PageSize.Should().Be(1);
            secondPage.TotalCount.Should().Be(2);
            secondPage.TotalPages.Should().Be(2);
            secondPage.Items.Should().ContainSingle();
            secondPage.Items.Single().Id.Should().Be(secondGameId);
            secondPage.Items.Single().StartsAt.Should().Be(secondStartsAt);

            firstPage.Items.Single().StartsAt.Should().BeBefore(secondPage.Items.Single().StartsAt);
        }

        [Fact]
        public async Task GetGames_ShouldReturnBadRequest_WhenPaginationIsInvalid()
        {
            var response = await _factory.CreateClient().GetAsync("/api/games?page=0&pageSize=101");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetGameById_ShouldReturnFrontendGameDetailsAndParticipants()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"game-details-organizer-{uniqueId}@test.com");
            var organizerProfileId = await CreatePlayerProfileAsync(organizerClient, "Game Details Organizer");

            var approvedPlayerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(approvedPlayerClient, $"game-details-approved-player-{uniqueId}@test.com");
            var approvedPlayerProfileId = await CreatePlayerProfileAsync(approvedPlayerClient, "Approved Detail Player");

            var pendingPlayerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(pendingPlayerClient, $"game-details-pending-player-{uniqueId}@test.com");
            var pendingPlayerProfileId = await CreatePlayerProfileAsync(pendingPlayerClient, "Pending Detail Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Game Details Court {uniqueId}");
            var gameId = await CreateGameAsync(organizerClient, courtId);

            var approvedParticipantId = await JoinGameAsync(approvedPlayerClient, gameId);

            var approveResponse = await organizerClient.PostAsync($"/api/game-participants/{approvedParticipantId}/approve", content: null);
            approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var pendingParticipantId = await JoinGameAsync(pendingPlayerClient, gameId);

            var response = await pendingPlayerClient.GetAsync($"/api/games/{gameId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseJson = await response.Content.ReadAsStringAsync();

            using var jsonDocument = JsonDocument.Parse(responseJson);
            var gameJson = jsonDocument.RootElement;

            AssertStringEnum(gameJson, "requiredLevel", nameof(GameLevel.Intermediate));
            AssertStringEnum(gameJson, "joinPolicy", nameof(GameJoinPolicy.ApprovalRequired));
            AssertStringEnum(gameJson, "status", nameof(GameStatus.Open));
            AssertStringEnum(gameJson, "currentUserJoinStatus", nameof(GameParticipantJoinStatus.PendingApproval));

            var courtJson = gameJson.GetProperty("court");
            AssertStringEnum(courtJson, "surfaceType", nameof(CourtSurfaceType.Indoor));

            var organizerJson = gameJson.GetProperty("organizer");
            AssertStringEnum(organizerJson, "skillLevel", nameof(PlayerSkillLevel.Intermediate));

            foreach (var participantJson in gameJson.GetProperty("participants").EnumerateArray())
            {
                AssertStringEnumProperty(participantJson, "skillLevel");
                AssertStringEnumProperty(participantJson, "joinStatus");
                AssertStringEnumProperty(participantJson, "attendanceStatus");
                AssertStringEnumProperty(participantJson, "offlinePaymentStatus");
            }

            var game = await response.Content.ReadFromApiJsonAsync<GameDetailsResponse>();

            game.Should().NotBeNull();
            game!.Id.Should().Be(gameId);

            game.Court.Id.Should().Be(courtId);
            game.Court.Name.Should().Be($"Game Details Court {uniqueId}");
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
            game.CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);

            game.Participants.Should().HaveCount(2);

            var approvedParticipant = game.Participants.Single(participant => participant.Id == approvedParticipantId);

            approvedParticipant.PlayerProfileId.Should().Be(approvedPlayerProfileId);
            approvedParticipant.DisplayName.Should().Be("Approved Detail Player");
            approvedParticipant.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);
            approvedParticipant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);

            var pendingParticipant = game.Participants.Single(participant => participant.Id == pendingParticipantId);

            pendingParticipant.PlayerProfileId.Should().Be(pendingPlayerProfileId);
            pendingParticipant.DisplayName.Should().Be("Pending Detail Player");
            pendingParticipant.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);
            pendingParticipant.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
        }

        [Fact]
        public async Task GetGameParticipants_ShouldReturnFrontendParticipantSummaries()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"participant-summary-organizer-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(organizerClient, "Participant Summary Organizer");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(playerClient, $"participant-summary-player-{uniqueId}@test.com");
            var playerProfileId = await CreatePlayerProfileAsync(playerClient, "Participant Summary Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Participant Summary Court {uniqueId}");
            var gameId = await CreateGameAsync(organizerClient, courtId);
            var participantId = await JoinGameAsync(playerClient, gameId);

            var response = await _factory.CreateClient().GetAsync($"/api/games/{gameId}/participants");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var participants = await response.Content.ReadFromApiJsonAsync<List<GameParticipantSummaryResponse>>();

            participants.Should().NotBeNull();
            participants.Should().ContainSingle();

            var participant = participants!.Single();

            participant.Id.Should().Be(participantId);
            participant.PlayerProfileId.Should().Be(playerProfileId);
            participant.DisplayName.Should().Be("Participant Summary Player");
            participant.SkillLevel.Should().Be(PlayerSkillLevel.Intermediate);
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.PendingApproval);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
        }

        private static void AssertStringEnum(JsonElement jsonElement, string propertyName, string expectedValue)
        {
            var property = jsonElement.GetProperty(propertyName);

            property.ValueKind.Should().Be(JsonValueKind.String);
            property.GetString().Should().Be(expectedValue);
        }

        private static void AssertStringEnumProperty(JsonElement jsonElement, string propertyName)
        {
            jsonElement.GetProperty(propertyName).ValueKind.Should().Be(JsonValueKind.String);
        }

        private static async Task RegisterAndAuthorizeAsync(HttpClient client, string email)
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

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        }

        private static async Task<Guid> CreatePlayerProfileAsync(HttpClient client, string displayName)
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

        private static async Task<Guid> CreateCourtAsync(HttpClient client, string name)
        {
            var response = await client.PostAsApiJsonAsync(
                "/api/courts",
                new
                {
                    Name = name,
                    Address = "Test Street 20",
                    Latitude = 52.3676,
                    Longitude = 4.9041,
                    SurfaceType = CourtSurfaceType.Indoor,
                    IsIndoor = true,
                    Description = "Court created by game summary integration test."
                });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateGameAsync(HttpClient client, Guid courtId, DateTimeOffset? startsAt = null)
        {
            var response = await client.PostAsApiJsonAsync("/api/games", CreateGamePayload(courtId, startsAt));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> JoinGameAsync(HttpClient client, Guid gameId)
        {
            var response = await client.PostAsync($"/api/games/{gameId}/participants", content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static object CreateGamePayload(Guid courtId, DateTimeOffset? startsAt = null)
        {
            var gameStartsAt = startsAt ?? DateTimeOffset.UtcNow.AddDays(1);

            return new
            {
                CourtId = courtId,
                StartsAt = gameStartsAt,
                EndsAt = gameStartsAt.AddHours(2),
                MaxPlayers = 12,
                PricePerPlayer = 15,
                RequiredLevel = GameLevel.Intermediate,
                JoinPolicy = GameJoinPolicy.ApprovalRequired,
                Description = "Game summary integration test game."
            };
        }

        private sealed record AuthResponse(Guid UserId, string Email, string AccessToken);

        private sealed record PagedResponse<T>(
            IReadOnlyList<T> Items,
            int Page,
            int PageSize,
            int TotalCount,
            int TotalPages);

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