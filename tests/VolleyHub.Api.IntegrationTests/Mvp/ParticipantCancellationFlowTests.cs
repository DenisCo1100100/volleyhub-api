using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class ParticipantCancellationFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ParticipantCancellationFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task LeaveGame_ShouldPersistOnTimeCancellationMetadata_WhenApprovedPlayerLeavesEarly()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"cancel-organizer-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(organizerClient, "Cancellation Organizer");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(playerClient, $"cancel-player-{uniqueId}@test.com");
            var playerProfileId = await CreatePlayerProfileAsync(playerClient, "Cancellation Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Cancellation Court {uniqueId}");
            var gameId = await CreateGameAsync(organizerClient, courtId, DateTimeOffset.UtcNow.AddDays(2));

            var participantId = await JoinGameAsync(playerClient, gameId);

            var approveResponse = await organizerClient.PostAsync($"/api/game-participants/{participantId}/approve", content: null);

            approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var beforeLeave = DateTimeOffset.UtcNow;

            var leaveResponse = await playerClient.PostAsync($"/api/games/{gameId}/participants/leave", content: null);

            var afterLeave = DateTimeOffset.UtcNow;

            leaveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var participant = await GetParticipantAsync(gameId, participantId);

            participant.PlayerProfileId.Should().Be(playerProfileId);
            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.CancellationType.Should().Be(GameParticipantCancellationType.OnTime);
            participant.CancelledAt.Should().NotBeNull();
            participant.CancelledAt!.Value.Should().BeOnOrAfter(beforeLeave);
            participant.CancelledAt.Value.Should().BeOnOrBefore(afterLeave);
            participant.RemovedAt.Should().BeNull();
        }

        [Fact]
        public async Task LeaveGame_ShouldWithdrawPendingRequestWithoutCancellationMetadata()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"withdraw-organizer-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(organizerClient, "Withdrawal Organizer");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(playerClient, $"withdraw-player-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(playerClient, "Withdrawal Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Withdrawal Court {uniqueId}");
            var gameId = await CreateGameAsync(organizerClient, courtId, DateTimeOffset.UtcNow.AddDays(2));

            var participantId = await JoinGameAsync(playerClient, gameId);

            var leaveResponse = await playerClient.PostAsync($"/api/games/{gameId}/participants/leave", content: null);

            leaveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var participant = await GetParticipantAsync(gameId, participantId);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.CancelledAt.Should().BeNull();
            participant.RemovedAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
        }

        [Fact]
        public async Task LeaveGame_ShouldReturnConflict_WhenApprovedPlayerLeavesAfterGameStart()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"started-organizer-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(organizerClient, "Started Game Organizer");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(playerClient, $"started-player-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(playerClient, "Started Game Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Started Game Court {uniqueId}");
            var gameId = await CreateGameAsync(organizerClient, courtId, DateTimeOffset.UtcNow.AddMinutes(-5));

            var participantId = await JoinGameAsync(playerClient, gameId);

            var approveResponse = await organizerClient.PostAsync($"/api/game-participants/{participantId}/approve", content: null);

            approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var leaveResponse = await playerClient.PostAsync($"/api/games/{gameId}/participants/leave", content: null);

            leaveResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var problemDetails = await leaveResponse.Content.ReadFromApiJsonAsync<ProblemDetailsResponse>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Status.Should().Be((int)HttpStatusCode.Conflict);
            problemDetails.Title.Should().Be("Conflict");
            problemDetails.Detail.Should().Be("Participation cannot be cancelled after the game has started.");

            var participant = await GetParticipantAsync(gameId, participantId);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
        }

        [Fact]
        public async Task RemoveParticipant_ShouldPersistRemovalMetadata_WhenOrganizerRemovesApprovedPlayer()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            var organizerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(organizerClient, $"remove-organizer-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(organizerClient, "Removal Organizer");

            var playerClient = _factory.CreateClient();
            await RegisterAndAuthorizeAsync(playerClient, $"remove-player-{uniqueId}@test.com");
            await CreatePlayerProfileAsync(playerClient, "Removal Player");

            var courtId = await CreateCourtAsync(organizerClient, $"Removal Court {uniqueId}");
            var gameId = await CreateGameAsync(organizerClient, courtId, DateTimeOffset.UtcNow.AddDays(2));

            var participantId = await JoinGameAsync(playerClient, gameId);

            var approveResponse = await organizerClient.PostAsync($"/api/game-participants/{participantId}/approve", content: null);

            approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var beforeRemoval = DateTimeOffset.UtcNow;

            var removeResponse = await organizerClient.DeleteAsync($"/api/game-participants/{participantId}");

            var afterRemoval = DateTimeOffset.UtcNow;

            removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var participant = await GetParticipantAsync(gameId, participantId);

            participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Removed);
            participant.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            participant.RemovedAt.Should().NotBeNull();
            participant.RemovedAt!.Value.Should().BeOnOrAfter(beforeRemoval);
            participant.RemovedAt.Value.Should().BeOnOrBefore(afterRemoval);
            participant.CancelledAt.Should().BeNull();
            participant.CancellationType.Should().BeNull();
        }

        private async Task<GameParticipantSummaryResponse> GetParticipantAsync(Guid gameId, Guid participantId)
        {
            var response = await _factory.CreateClient().GetAsync($"/api/games/{gameId}/participants");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var participants = await response.Content.ReadFromApiJsonAsync<List<GameParticipantSummaryResponse>>();

            participants.Should().NotBeNull();

            return participants!.Single(participant => participant.Id == participantId);
        }

        private static async Task RegisterAndAuthorizeAsync(HttpClient client, string email)
        {
            const string password = "Password123!";

            var registerResponse = await client.PostAsApiJsonAsync("/api/auth/register", new
            {
                Email = email,
                Password = password
            });

            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var loginResponse = await client.PostAsApiJsonAsync("/api/auth/login", new
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
            var response = await client.PostAsApiJsonAsync("/api/player-profiles", new
            {
                DisplayName = displayName,
                SkillLevel = PlayerSkillLevel.Intermediate,
                City = "Amsterdam",
                Bio = "Participant cancellation integration test profile."
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateCourtAsync(HttpClient client, string name)
        {
            var response = await client.PostAsApiJsonAsync("/api/courts", new
            {
                Name = name,
                Address = "Cancellation Test Street 1",
                Latitude = 52.3676,
                Longitude = 4.9041,
                SurfaceType = CourtSurfaceType.Indoor,
                IsIndoor = true,
                Description = "Court created by participant cancellation integration tests."
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> CreateGameAsync(HttpClient client, Guid courtId, DateTimeOffset startsAt)
        {
            var response = await client.PostAsApiJsonAsync("/api/games", new
            {
                CourtId = courtId,
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(2),
                MaxPlayers = 12,
                PricePerPlayer = 15,
                RequiredLevel = GameLevel.Intermediate,
                JoinPolicy = GameJoinPolicy.ApprovalRequired,
                Description = "Participant cancellation integration test game."
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> JoinGameAsync(HttpClient client, Guid gameId)
        {
            var response = await client.PostAsync($"/api/games/{gameId}/participants", content: null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private sealed record AuthResponse(Guid UserId, string Email, string AccessToken);

        private sealed record ProblemDetailsResponse(int Status, string Title, string Detail, string Instance);

        private sealed record GameParticipantSummaryResponse(
            Guid Id,
            Guid PlayerProfileId,
            string DisplayName,
            PlayerSkillLevel SkillLevel,
            GameParticipantJoinStatus JoinStatus,
            GameParticipantAttendanceStatus AttendanceStatus,
            GameParticipantOfflinePaymentStatus OfflinePaymentStatus,
            DateTimeOffset JoinedAt,
            DateTimeOffset? ApprovedAt,
            DateTimeOffset? CancelledAt,
            DateTimeOffset? RemovedAt,
            GameParticipantCancellationType? CancellationType);
    }
}