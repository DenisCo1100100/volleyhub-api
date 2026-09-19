using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class GameWaitlistFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GameWaitlistFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(GameJoinPolicy.Open, false)]
        [InlineData(GameJoinPolicy.ApprovalRequired, true)]
        public async Task Waitlist_ShouldFillReleasedPlaceInOrder(GameJoinPolicy policy, bool organizerRemoves)
        {
            var (organizer, gameId, players) = await CreateFullGameAsync(policy);
            using var first = await CreatePlayerAsync();
            using var second = await CreatePlayerAsync();
            var firstId = await JoinWaitlistAsync(first, gameId);
            var secondId = await JoinWaitlistAsync(second, gameId);

            var details = await GetDetailsAsync(first, gameId);
            details.Status.Should().Be(GameStatus.Full);
            details.ApprovedParticipantCount.Should().Be(2);
            details.PendingParticipantCount.Should().Be(0);
            details.AvailableSpots.Should().Be(0);
            details.WaitlistedParticipantCount.Should().Be(2);
            details.CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.Waitlisted);
            details.Participants.Single(participant => participant.Id == firstId).WaitlistPosition.Should().Be(1);
            details.Participants.Single(participant => participant.Id == secondId).WaitlistPosition.Should().Be(2);

            var playerParticipantId = details.Participants.First(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved).Id;
            var release = organizerRemoves
                ? await organizer.DeleteAsync($"/api/game-participants/{playerParticipantId}")
                : await players[0].PostAsync($"/api/games/{gameId}/participants/leave", null);
            release.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var released = await GetDetailsAsync(first, gameId);
            released.Status.Should().Be(GameStatus.Open);
            released.AvailableSpots.Should().Be(1);
            released.WaitlistedParticipantCount.Should().Be(2);

            var promotion = await organizer.PostAsync($"/api/games/{gameId}/waitlist/promote", null);
            promotion.StatusCode.Should().Be(HttpStatusCode.OK);
            (await promotion.Content.ReadFromApiJsonAsync<Guid>()).Should().Be(firstId);

            var filled = await GetDetailsAsync(first, gameId);
            filled.Status.Should().Be(GameStatus.Full);
            filled.AvailableSpots.Should().Be(0);
            filled.ApprovedParticipantCount.Should().Be(2);
            filled.WaitlistedParticipantCount.Should().Be(1);
            var promoted = filled.Participants.Single(participant => participant.Id == firstId);
            promoted.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            promoted.ApprovedAt.Should().NotBeNull();
            promoted.WaitlistPosition.Should().BeNull();
            promoted.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
            filled.Participants.Single(participant => participant.Id == secondId).WaitlistPosition.Should().Be(1);

            (await organizer.PostAsync($"/api/games/{gameId}/waitlist/promote", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Waitlist_ShouldSupportWithdrawalAndOrganizerRejectionWithoutCancellationMetadata()
        {
            var (organizer, gameId, _) = await CreateFullGameAsync();
            using var first = await CreatePlayerAsync();
            using var second = await CreatePlayerAsync();
            var firstId = await JoinWaitlistAsync(first, gameId);
            var secondId = await JoinWaitlistAsync(second, gameId);

            (await first.PostAsync($"/api/games/{gameId}/participants/leave", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await organizer.PostAsync($"/api/game-participants/{secondId}/reject", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            var details = await GetDetailsAsync(first, gameId);
            details.Status.Should().Be(GameStatus.Full);
            details.ApprovedParticipantCount.Should().Be(2);
            details.WaitlistedParticipantCount.Should().Be(0);
            var withdrawn = details.Participants.Single(participant => participant.Id == firstId);
            withdrawn.JoinStatus.Should().Be(GameParticipantJoinStatus.Cancelled);
            withdrawn.CancelledAt.Should().BeNull();
            withdrawn.CancellationType.Should().BeNull();
            withdrawn.WaitlistPosition.Should().BeNull();
            details.Participants.Single(participant => participant.Id == secondId).JoinStatus.Should().Be(GameParticipantJoinStatus.Rejected);
            (await first.PostAsync($"/api/games/{gameId}/waitlist", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Waitlist_ShouldRejectUnauthorizedForbiddenInvalidAndMissingRequests()
        {
            var (organizer, gameId, _) = await CreateFullGameAsync();
            using var anonymous = _factory.CreateClient();
            using var outsider = await CreatePlayerAsync();
            using var noProfile = await CreatePlayerAsync(false);

            foreach (var suffix in new[] { "waitlist", "waitlist/promote" })
            {
                (await anonymous.PostAsync($"/api/games/{gameId}/{suffix}", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                (await noProfile.PostAsync($"/api/games/{gameId}/{suffix}", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
                (await organizer.PostAsync($"/api/games/{Guid.Empty}/{suffix}", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
                (await organizer.PostAsync($"/api/games/{Guid.NewGuid()}/{suffix}", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            }

            var forbidden = await outsider.PostAsync($"/api/games/{gameId}/waitlist/promote", null);
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            forbidden.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        [Theory]
        [InlineData("cancel")]
        [InlineData("complete")]
        public async Task Waitlist_ShouldRejectJoiningAndPromotionForTerminalGames(string action)
        {
            var (organizer, gameId, _) = await CreateFullGameAsync();
            using var player = await CreatePlayerAsync();
            await JoinWaitlistAsync(player, gameId);
            (await organizer.PostAsync($"/api/games/{gameId}/{action}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            using var another = await CreatePlayerAsync();
            (await another.PostAsync($"/api/games/{gameId}/waitlist", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await organizer.PostAsync($"/api/games/{gameId}/waitlist/promote", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Waitlist_ShouldRejectJoiningAndPromotionAfterStart()
        {
            var (organizer, gameId, players) = await CreateFullGameAsync();
            using var player = await CreatePlayerAsync();
            await JoinWaitlistAsync(player, gameId);
            (await players[0].PostAsync($"/api/games/{gameId}/participants/leave", null)).EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var game = await context.Games.SingleAsync(game => game.Id == gameId);
                context.Entry(game).Property(game => game.StartsAt).CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
                await context.SaveChangesAsync();
            }

            (await organizer.PostAsync($"/api/games/{gameId}/waitlist/promote", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            using var another = await CreatePlayerAsync();
            (await another.PostAsync($"/api/games/{gameId}/waitlist", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Waitlist_ShouldRejectDuplicateAndDirectApproval_AndPreventQueueBypass()
        {
            var (organizer, gameId, players) = await CreateFullGameAsync();
            using var player = await CreatePlayerAsync();
            var id = await JoinWaitlistAsync(player, gameId);
            (await player.PostAsync($"/api/games/{gameId}/waitlist", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await players[0].PostAsync($"/api/games/{gameId}/participants/leave", null)).EnsureSuccessStatusCode();

            (await organizer.PostAsync($"/api/game-participants/{id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            using var newcomer = await CreatePlayerAsync();
            (await newcomer.PostAsync($"/api/games/{gameId}/participants", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);

            var participantsResponse = await player.GetAsync($"/api/games/{gameId}/participants");
            var participants = await participantsResponse.Content.ReadFromApiJsonAsync<List<ParticipantResponse>>();
            participants!.Single(participant => participant.Id == id).WaitlistPosition.Should().Be(1);
        }

        private async Task<(HttpClient Organizer, Guid GameId, HttpClient[] Players)> CreateFullGameAsync(GameJoinPolicy policy = GameJoinPolicy.Open)
        {
            var organizer = await CreatePlayerAsync();
            var courtResponse = await organizer.PostAsApiJsonAsync("/api/courts", new
            {
                Name = "Waitlist Court", Address = "Test Street 1", Latitude = 53.9, Longitude = 27.56,
                SurfaceType = CourtSurfaceType.Indoor, IsIndoor = true
            });
            courtResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var courtId = await courtResponse.Content.ReadFromApiJsonAsync<Guid>();
            var gameResponse = await organizer.PostAsApiJsonAsync("/api/games", new
            {
                CourtId = courtId, StartsAt = DateTimeOffset.UtcNow.AddDays(2), MaxPlayers = 2,
                PricePerPlayer = 15, RequiredLevel = GameLevel.Intermediate, JoinPolicy = policy
            });
            gameResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var gameId = await gameResponse.Content.ReadFromApiJsonAsync<Guid>();
            var players = new[] { await CreatePlayerAsync(), await CreatePlayerAsync() };
            foreach (var player in players)
            {
                var joinResponse = await player.PostAsync($"/api/games/{gameId}/participants", null);
                joinResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                if (policy is GameJoinPolicy.ApprovalRequired)
                {
                    var id = await joinResponse.Content.ReadFromApiJsonAsync<Guid>();
                    (await organizer.PostAsync($"/api/game-participants/{id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
                }
            }
            return (organizer, gameId, players);
        }

        private async Task<HttpClient> CreatePlayerAsync(bool withProfile = true)
        {
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"waitlist-{Guid.NewGuid():N}@test.com", "Unused test password hash");
            context.Users.Add(user);
            if (withProfile)
            {
                context.PlayerProfiles.Add(PlayerProfile.Create(user.Id, "Waitlist Player", PlayerSkillLevel.Intermediate, "Minsk", null));
            }
            await context.SaveChangesAsync();
            var token = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<Guid> JoinWaitlistAsync(HttpClient client, Guid gameId)
        {
            var response = await client.PostAsync($"/api/games/{gameId}/waitlist", null);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<GameResponse> GetDetailsAsync(HttpClient client, Guid gameId)
        {
            var response = await client.GetAsync($"/api/games/{gameId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromApiJsonAsync<GameResponse>())!;
        }

        private sealed record GameResponse(GameStatus Status, int ApprovedParticipantCount, int PendingParticipantCount, int AvailableSpots,
            int WaitlistedParticipantCount, GameParticipantJoinStatus? CurrentUserJoinStatus, IReadOnlyList<ParticipantResponse> Participants);

        private sealed record ParticipantResponse(Guid Id, GameParticipantJoinStatus JoinStatus, DateTimeOffset? ApprovedAt,
            DateTimeOffset? CancelledAt, GameParticipantCancellationType? CancellationType, int? WaitlistPosition,
            GameParticipantOfflinePaymentStatus OfflinePaymentStatus);
    }
}
