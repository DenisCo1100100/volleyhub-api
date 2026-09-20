using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Common;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class OfflinePaymentFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public OfflinePaymentFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Organizer_ShouldRecordAndCorrectPaymentsBeforeAndAfterCompletion(bool complete)
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            var gameId = await CreateGameAsync(organizer);
            var participantId = await JoinAsync(player, gameId);
            if (complete)
            {
                (await organizer.PostAsync($"/api/games/{gameId}/complete", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
                (await organizer.PostAsApiJsonAsync($"/api/game-participants/{participantId}/attendance",
                    new { AttendanceStatus = GameParticipantAttendanceStatus.Absent })).StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            foreach (var status in new[] { GameParticipantOfflinePaymentStatus.Paid, GameParticipantOfflinePaymentStatus.Paid,
                GameParticipantOfflinePaymentStatus.Pending, GameParticipantOfflinePaymentStatus.Paid })
            {
                (await SetPaymentAsync(organizer, participantId, status)).StatusCode.Should().Be(HttpStatusCode.NoContent);
                var summary = await SummaryAsync(organizer, gameId);
                summary.ExpectedParticipantCount.Should().Be(1);
                summary.ExpectedAmount.Should().Be(12.35m);
                summary.PaidParticipantCount.Should().Be(status is GameParticipantOfflinePaymentStatus.Paid ? 1 : 0);
                summary.OutstandingParticipantCount.Should().Be(1 - summary.PaidParticipantCount);
                summary.PaidAmount.Should().Be(status is GameParticipantOfflinePaymentStatus.Paid ? 12.35m : 0);
                summary.OutstandingAmount.Should().Be(summary.ExpectedAmount - summary.PaidAmount);
                var details = await DetailsAsync(player, gameId);
                var participant = details.Participants.Single();
                participant.OfflinePaymentStatus.Should().Be(status);
                participant.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
                participant.AttendanceStatus.Should().Be(complete ? GameParticipantAttendanceStatus.Absent : GameParticipantAttendanceStatus.NotMarked);
                var listResponse = await organizer.GetAsync($"/api/games/{gameId}/participants");
                var list = await listResponse.Content.ReadFromApiJsonAsync<GameParticipantSummaryDto[]>();
                list!.Single().OfflinePaymentStatus.Should().Be(status);
            }
        }

        [Fact]
        public async Task PaymentEndpoints_ShouldEnforceOwnershipAuthenticationAndValidation()
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            using var outsider = await CreatePlayerAsync();
            using var noProfile = await CreatePlayerAsync(false);
            using var anonymous = _factory.CreateClient();
            var gameId = await CreateGameAsync(organizer);
            var participantId = await JoinAsync(player, gameId);

            foreach (var (client, expected) in new[]
            {
                (anonymous, HttpStatusCode.Unauthorized), (player, HttpStatusCode.Forbidden),
                (outsider, HttpStatusCode.Forbidden), (noProfile, HttpStatusCode.NotFound)
            })
            {
                (await SetPaymentAsync(client, participantId)).StatusCode.Should().Be(expected);
                (await client.GetAsync($"/api/games/{gameId}/offline-payments")).StatusCode.Should().Be(expected);
            }

            (await SetPaymentAsync(organizer, Guid.NewGuid())).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SetPaymentAsync(organizer, Guid.Empty)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await organizer.GetAsync($"/api/games/{Guid.NewGuid()}/offline-payments")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await organizer.GetAsync($"/api/games/{Guid.Empty}/offline-payments")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            foreach (var body in new[] { "{}", "{\"offlinePaymentStatus\":\"Unknown\"}", "{\"offlinePaymentStatus\":\"Invalid\"}", "{\"offlinePaymentStatus\":3}" })
            {
                var response = await organizer.PutAsync($"/api/game-participants/{participantId}/offline-payment",
                    new StringContent(body, Encoding.UTF8, "application/json"));
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            }

            (await DetailsAsync(organizer, gameId)).Participants.Single().OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
            var invalidFreeStatus = await SetPaymentAsync(organizer, participantId, GameParticipantOfflinePaymentStatus.NotRequired);
            invalidFreeStatus.StatusCode.Should().Be(HttpStatusCode.Conflict);
            invalidFreeStatus.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        [Fact]
        public async Task FreeGame_ShouldRemainNotRequiredAndHaveNoExpectedPayments()
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            var gameId = await CreateGameAsync(organizer, price: 0);
            var participantId = await JoinAsync(player, gameId);

            (await SetPaymentAsync(organizer, participantId)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SetPaymentAsync(organizer, participantId, GameParticipantOfflinePaymentStatus.Pending)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SetPaymentAsync(organizer, participantId, GameParticipantOfflinePaymentStatus.NotRequired)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await DetailsAsync(player, gameId)).Participants.Single().OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.NotRequired);
            var summary = await SummaryAsync(organizer, gameId);
            summary.ExpectedParticipantCount.Should().Be(0);
            summary.PaidParticipantCount.Should().Be(0);
            summary.OutstandingParticipantCount.Should().Be(0);
            summary.ExpectedAmount.Should().Be(0);
        }

        [Theory]
        [InlineData("pending")]
        [InlineData("rejected")]
        [InlineData("cancelled")]
        [InlineData("removed")]
        public async Task Payment_ShouldRejectParticipantsWithoutConfirmedPlaces(string state)
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            var gameId = await CreateGameAsync(organizer, policy: state is "pending" or "rejected" ? GameJoinPolicy.ApprovalRequired : GameJoinPolicy.Open);
            var participantId = await JoinAsync(player, gameId);
            if (state is "rejected") (await organizer.PostAsync($"/api/game-participants/{participantId}/reject", null)).EnsureSuccessStatusCode();
            if (state is "cancelled") (await player.PostAsync($"/api/games/{gameId}/participants/leave", null)).EnsureSuccessStatusCode();
            if (state is "removed") (await organizer.DeleteAsync($"/api/game-participants/{participantId}")).EnsureSuccessStatusCode();

            (await SetPaymentAsync(organizer, participantId)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SummaryAsync(organizer, gameId)).ExpectedParticipantCount.Should().Be(0);

            if (state is "pending")
            {
                (await organizer.PostAsync($"/api/game-participants/{participantId}/approve", null)).EnsureSuccessStatusCode();
                (await SetPaymentAsync(organizer, participantId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
                (await SummaryAsync(organizer, gameId)).PaidParticipantCount.Should().Be(1);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PaidParticipantRelease_ShouldPreserveHistoryAndAllowWaitlistPaymentAfterPromotion(bool remove)
        {
            using var organizer = await CreatePlayerAsync();
            using var first = await CreatePlayerAsync();
            using var second = await CreatePlayerAsync();
            using var queued = await CreatePlayerAsync();
            var gameId = await CreateGameAsync(organizer, maxPlayers: 2);
            var firstId = await JoinAsync(first, gameId);
            await JoinAsync(second, gameId);
            var queuedId = await JoinAsync(queued, gameId, waitlist: true);
            (await SetPaymentAsync(organizer, firstId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await SetPaymentAsync(organizer, queuedId)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await SummaryAsync(organizer, gameId)).ExpectedParticipantCount.Should().Be(2);

            var release = remove ? await organizer.DeleteAsync($"/api/game-participants/{firstId}")
                : await first.PostAsync($"/api/games/{gameId}/participants/leave", null);
            release.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var released = await DetailsAsync(organizer, gameId);
            released.Status.Should().Be(GameStatus.Open);
            var history = released.Participants.Single(participant => participant.Id == firstId);
            history.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            if (remove) history.RemovedAt.Should().NotBeNull();
            else history.CancelledAt.Should().NotBeNull();
            (await SummaryAsync(organizer, gameId)).PaidParticipantCount.Should().Be(0);
            (await SetPaymentAsync(organizer, firstId, GameParticipantOfflinePaymentStatus.Pending)).StatusCode.Should().Be(HttpStatusCode.Conflict);

            (await organizer.PostAsync($"/api/games/{gameId}/waitlist/promote", null)).StatusCode.Should().Be(HttpStatusCode.OK);
            var promoted = await DetailsAsync(organizer, gameId);
            promoted.Status.Should().Be(GameStatus.Full);
            promoted.Participants.Single(participant => participant.Id == queuedId).OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Pending);
            (await SetPaymentAsync(organizer, queuedId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            var summary = await SummaryAsync(organizer, gameId);
            summary.ExpectedParticipantCount.Should().Be(2);
            summary.PaidParticipantCount.Should().Be(1);
            summary.OutstandingParticipantCount.Should().Be(1);
        }

        [Fact]
        public async Task CancelledGame_ShouldKeepPaymentHistoryButHaveNoActivePaymentTotals()
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            var gameId = await CreateGameAsync(organizer);
            var participantId = await JoinAsync(player, gameId);
            (await SetPaymentAsync(organizer, participantId)).EnsureSuccessStatusCode();
            (await organizer.PostAsync($"/api/games/{gameId}/cancel", null)).EnsureSuccessStatusCode();

            (await SetPaymentAsync(organizer, participantId, GameParticipantOfflinePaymentStatus.Pending)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailsAsync(organizer, gameId)).Participants.Single().OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            var summary = await SummaryAsync(organizer, gameId);
            summary.ExpectedParticipantCount.Should().Be(0);
            summary.PaidAmount.Should().Be(0);
            summary.OutstandingAmount.Should().Be(0);
        }

        [Fact]
        public async Task PriceChange_ShouldSynchronizeRequestsAndConfirmedPlacesAndProtectRecordedPayments()
        {
            using var organizer = await CreatePlayerAsync();
            using var approved = await CreatePlayerAsync();
            using var pending = await CreatePlayerAsync();
            var gameId = await CreateGameAsync(organizer, policy: GameJoinPolicy.ApprovalRequired);
            var approvedId = await JoinAsync(approved, gameId);
            await JoinAsync(pending, gameId);
            (await organizer.PostAsync($"/api/game-participants/{approvedId}/approve", null)).EnsureSuccessStatusCode();

            (await ChangePriceAsync(organizer, gameId, 0)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await DetailsAsync(organizer, gameId)).Participants.Should().OnlyContain(participant =>
                participant.OfflinePaymentStatus == GameParticipantOfflinePaymentStatus.NotRequired);
            (await SummaryAsync(organizer, gameId)).ExpectedParticipantCount.Should().Be(0);

            (await ChangePriceAsync(organizer, gameId, 20.25m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await DetailsAsync(organizer, gameId)).Participants.Should().OnlyContain(participant =>
                participant.OfflinePaymentStatus == GameParticipantOfflinePaymentStatus.Pending);
            (await SetPaymentAsync(organizer, approvedId)).EnsureSuccessStatusCode();
            (await ChangePriceAsync(organizer, gameId, 30)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ChangePriceAsync(organizer, gameId, 0)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ChangePriceAsync(organizer, gameId, 20.25m)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await SummaryAsync(organizer, gameId)).PaidAmount.Should().Be(20.25m);
        }

        private async Task<HttpClient> CreatePlayerAsync(bool withProfile = true)
        {
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"payment-{Guid.NewGuid():N}@test.com", "Unused test password hash");
            context.Users.Add(user);
            if (withProfile) context.PlayerProfiles.Add(PlayerProfile.Create(user.Id, "Payment Player", PlayerSkillLevel.Intermediate, "Minsk", null));
            await context.SaveChangesAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user));
            return client;
        }

        private static async Task<Guid> CreateGameAsync(HttpClient organizer, decimal price = 12.35m, GameJoinPolicy policy = GameJoinPolicy.Open, int maxPlayers = 12)
        {
            var court = await organizer.PostAsApiJsonAsync("/api/courts", new
            {
                Name = "Payment Court", Address = "Test Street 1", Latitude = 53.9, Longitude = 27.56,
                SurfaceType = CourtSurfaceType.Indoor, IsIndoor = true
            });
            court.StatusCode.Should().Be(HttpStatusCode.Created);
            var courtId = await court.Content.ReadFromApiJsonAsync<Guid>();
            var game = await organizer.PostAsApiJsonAsync("/api/games", new
            {
                CourtId = courtId, StartsAt = DateTimeOffset.UtcNow.AddDays(2), MaxPlayers = maxPlayers,
                PricePerPlayer = price, RequiredLevel = GameLevel.Any, JoinPolicy = policy
            });
            game.StatusCode.Should().Be(HttpStatusCode.Created);
            return await game.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<Guid> JoinAsync(HttpClient player, Guid gameId, bool waitlist = false)
        {
            var response = await player.PostAsync($"/api/games/{gameId}/{(waitlist ? "waitlist" : "participants")}", null);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static Task<HttpResponseMessage> SetPaymentAsync(HttpClient client, Guid participantId,
            GameParticipantOfflinePaymentStatus status = GameParticipantOfflinePaymentStatus.Paid) =>
            client.PutAsApiJsonAsync($"/api/game-participants/{participantId}/offline-payment", new { OfflinePaymentStatus = status });

        private static async Task<GameOfflinePaymentSummaryDto> SummaryAsync(HttpClient organizer, Guid gameId)
        {
            var response = await organizer.GetAsync($"/api/games/{gameId}/offline-payments");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromApiJsonAsync<GameOfflinePaymentSummaryDto>())!;
        }

        private static async Task<GameDetailsDto> DetailsAsync(HttpClient client, Guid gameId)
        {
            var response = await client.GetAsync($"/api/games/{gameId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromApiJsonAsync<GameDetailsDto>())!;
        }

        private static async Task<HttpResponseMessage> ChangePriceAsync(HttpClient organizer, Guid gameId, decimal price)
        {
            var game = await DetailsAsync(organizer, gameId);
            return await organizer.PutAsApiJsonAsync($"/api/games/{gameId}", new
            {
                CourtId = game.Court.Id, game.StartsAt, game.EndsAt, game.MaxPlayers, PricePerPlayer = price,
                game.RequiredLevel, game.JoinPolicy, game.Description
            });
        }
    }
}
