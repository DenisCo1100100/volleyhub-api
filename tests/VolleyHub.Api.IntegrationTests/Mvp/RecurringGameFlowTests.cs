using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.GameRecurrences.Common;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class RecurringGameFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RecurringGameFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_ShouldSnapshotSettingsWithoutParticipantsAndRejectRetry()
        {
            using var organizer = await PlayerAsync();
            using var player = await PlayerAsync();
            var sourceId = await SourceAsync(organizer);
            await JoinAsync(player, sourceId);
            var id = Guid.NewGuid();
            var firstStartsAt = DateTimeOffset.UtcNow.AddDays(8).ToOffset(TimeSpan.FromHours(3));
            var request = new { Id = id, SourceGameId = sourceId, FirstStartsAt = firstStartsAt, OccurrenceCount = 3 };
            var response = await organizer.PostAsApiJsonAsync("/api/game-recurrences", request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location!.AbsolutePath.Should().Be($"/api/game-recurrences/{id}");
            var recurrence = await RecurrenceAsync(organizer, id);
            recurrence.Occurrences.Should().HaveCount(3);
            recurrence.Duration.Should().Be(TimeSpan.FromHours(2));
            foreach (var occurrence in recurrence.Occurrences)
            {
                var game = await DetailsAsync(organizer, occurrence.GameId);
                game.RecurrenceId.Should().Be(id);
                game.OccurrenceNumber.Should().Be(occurrence.OccurrenceNumber);
                game.StartsAt.Should().Be(firstStartsAt.AddDays(7 * (occurrence.OccurrenceNumber - 1)));
                game.Participants.Should().BeEmpty();
                game.Description.Should().Be("Weekly game");
            }
            (await organizer.PostAsApiJsonAsync("/api/game-recurrences", request)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailsAsync(organizer, sourceId)).Participants.Should().ContainSingle();
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Games.CountAsync(game => game.RecurrenceId == id)).Should().Be(3);

            var historyResponse = await organizer.GetAsync("/api/player-profiles/me/organized-games?pageSize=100");
            historyResponse.EnsureSuccessStatusCode();
            var history = await historyResponse.Content.ReadFromApiJsonAsync<PagedResult<OrganizedGameHistoryDto>>();
            history!.Items.Where(item => item.Game.RecurrenceId == id).Should().HaveCount(3);
        }

        [Fact]
        public async Task SingleAndFutureEdits_ShouldHaveExplicitIndependentScopes()
        {
            using var organizer = await PlayerAsync();
            var recurrence = await CreateAsync(organizer, await SourceAsync(organizer), count: 4);
            var first = await DetailsAsync(organizer, recurrence.Occurrences[0].GameId);
            (await organizer.PutAsApiJsonAsync($"/api/games/{first.Id}", new
            {
                CourtId = first.Court.Id, StartsAt = first.StartsAt.AddDays(1), EndsAt = first.EndsAt!.Value.AddDays(1),
                first.MaxPlayers, first.PricePerPlayer, first.RequiredLevel, first.JoinPolicy, Description = "One game only"
            })).StatusCode.Should().Be(HttpStatusCode.NoContent);
            var cancelledId = recurrence.Occurrences[2].GameId;
            (await organizer.PostAsync($"/api/games/{cancelledId}/cancel", null)).EnsureSuccessStatusCode();
            (await UpdateFutureAsync(organizer, recurrence, from: 2, price: 0)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await DetailsAsync(organizer, first.Id)).Description.Should().Be("One game only");
            (await DetailsAsync(organizer, recurrence.Occurrences[1].GameId)).PricePerPlayer.Should().Be(0);
            (await DetailsAsync(organizer, cancelledId)).PricePerPlayer.Should().Be(15);
            (await DetailsAsync(organizer, recurrence.Occurrences[3].GameId)).Description.Should().Be("New series settings");
            var updated = await RecurrenceAsync(organizer, recurrence.Id);
            updated.Occurrences[0].ScheduledStartsAt.Should().Be(first.StartsAt);
            updated.Occurrences[0].StartsAt.Should().Be(first.StartsAt.AddDays(1));
            updated.PricePerPlayer.Should().Be(0);
            updated.CancelledAt.Should().BeNull();
        }

        [Fact]
        public async Task FutureEdit_ShouldLeaveAllGamesUnchangedWhenLaterOccurrenceHasPayment()
        {
            using var organizer = await PlayerAsync();
            using var player = await PlayerAsync();
            var recurrence = await CreateAsync(organizer, await SourceAsync(organizer));
            var participantId = await JoinAsync(player, recurrence.Occurrences[1].GameId);
            (await organizer.PutAsApiJsonAsync($"/api/game-participants/{participantId}/offline-payment",
                new { OfflinePaymentStatus = GameParticipantOfflinePaymentStatus.Paid })).EnsureSuccessStatusCode();

            (await UpdateFutureAsync(organizer, recurrence, price: 0)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            foreach (var occurrence in recurrence.Occurrences)
                (await DetailsAsync(organizer, occurrence.GameId)).PricePerPlayer.Should().Be(15);
            (await RecurrenceAsync(organizer, recurrence.Id)).PricePerPlayer.Should().Be(15);
        }

        [Fact]
        public async Task FuturePriceEdit_ShouldSynchronizePendingAndApprovedParticipants()
        {
            using var organizer = await PlayerAsync();
            using var approved = await PlayerAsync();
            using var pending = await PlayerAsync();
            var recurrence = await CreateAsync(organizer, await SourceAsync(organizer, policy: GameJoinPolicy.ApprovalRequired));
            var gameId = recurrence.Occurrences[0].GameId;
            var approvedId = await JoinAsync(approved, gameId);
            await JoinAsync(pending, gameId);
            (await organizer.PostAsync($"/api/game-participants/{approvedId}/approve", null)).EnsureSuccessStatusCode();
            (await UpdateFutureAsync(organizer, recurrence, price: 0)).EnsureSuccessStatusCode();
            (await DetailsAsync(organizer, gameId)).Participants.Should().OnlyContain(p => p.OfflinePaymentStatus == GameParticipantOfflinePaymentStatus.NotRequired);
            (await UpdateFutureAsync(organizer, recurrence, price: 25)).EnsureSuccessStatusCode();
            (await DetailsAsync(organizer, gameId)).Participants.Should().OnlyContain(p => p.OfflinePaymentStatus == GameParticipantOfflinePaymentStatus.Pending);
        }

        [Fact]
        public async Task GeneratedGames_ShouldKeepWaitlistCancellationAttendancePaymentsAndHistory()
        {
            using var organizer = await PlayerAsync();
            using var firstPlayer = await PlayerAsync();
            using var secondPlayer = await PlayerAsync();
            using var queuedPlayer = await PlayerAsync();
            var recurrence = await CreateAsync(organizer, await SourceAsync(organizer, maxPlayers: 2));
            var gameId = recurrence.Occurrences[0].GameId;
            var firstId = await JoinAsync(firstPlayer, gameId);
            await JoinAsync(secondPlayer, gameId);
            var queuedId = await JoinAsync(queuedPlayer, gameId, waitlist: true);
            (await UpdateFutureAsync(organizer, recurrence)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await firstPlayer.PostAsync($"/api/games/{gameId}/participants/leave", null)).EnsureSuccessStatusCode();
            (await organizer.PostAsync($"/api/games/{gameId}/waitlist/promote", null)).EnsureSuccessStatusCode();
            (await organizer.PutAsApiJsonAsync($"/api/game-participants/{queuedId}/offline-payment",
                new { OfflinePaymentStatus = GameParticipantOfflinePaymentStatus.Paid })).EnsureSuccessStatusCode();
            (await organizer.PostAsync($"/api/games/{gameId}/complete", null)).EnsureSuccessStatusCode();
            (await organizer.PostAsApiJsonAsync($"/api/game-participants/{queuedId}/attendance",
                new { AttendanceStatus = GameParticipantAttendanceStatus.Present })).EnsureSuccessStatusCode();

            (await organizer.PostAsync($"/api/game-recurrences/{recurrence.Id}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            var completed = await DetailsAsync(organizer, gameId);
            completed.Status.Should().Be(GameStatus.Completed);
            completed.Participants.Single(p => p.Id == firstId).CancellationType.Should().Be(GameParticipantCancellationType.OnTime);
            completed.Participants.Single(p => p.Id == queuedId).AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
            completed.Participants.Single(p => p.Id == queuedId).OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            (await DetailsAsync(organizer, recurrence.Occurrences[1].GameId)).Status.Should().Be(GameStatus.Cancelled);
            (await DetailsAsync(organizer, recurrence.SourceGameId)).Status.Should().Be(GameStatus.Open);
            var history = await queuedPlayer.GetAsync("/api/player-profiles/me/games");
            history.EnsureSuccessStatusCode();
            var items = (await history.Content.ReadFromApiJsonAsync<PagedResult<PlayerGameHistoryDto>>())!.Items;
            items.Should().ContainSingle(item => item.Game.Id == gameId && item.Game.RecurrenceId == recurrence.Id);
        }

        [Fact]
        public async Task CancelSeries_ShouldKeepStartedGamesAndParticipantRecordsAndBeIdempotent()
        {
            using var organizer = await PlayerAsync();
            using var player = await PlayerAsync();
            var recurrence = await CreateAsync(organizer, await SourceAsync(organizer));
            var first = await DetailsAsync(organizer, recurrence.Occurrences[0].GameId);
            (await organizer.PutAsApiJsonAsync($"/api/games/{first.Id}", new
            {
                CourtId = first.Court.Id, StartsAt = DateTimeOffset.UtcNow.AddHours(-1), EndsAt = (DateTimeOffset?)null,
                first.MaxPlayers, first.PricePerPlayer, first.RequiredLevel, first.JoinPolicy, first.Description
            })).EnsureSuccessStatusCode();
            var participantId = await JoinAsync(player, recurrence.Occurrences[1].GameId);
            (await organizer.PutAsApiJsonAsync($"/api/game-participants/{participantId}/offline-payment",
                new { OfflinePaymentStatus = GameParticipantOfflinePaymentStatus.Paid })).EnsureSuccessStatusCode();
            (await organizer.PostAsync($"/api/game-recurrences/{recurrence.Id}/cancel", null)).EnsureSuccessStatusCode();
            var cancelledAt = (await RecurrenceAsync(organizer, recurrence.Id)).CancelledAt;
            (await organizer.PostAsync($"/api/game-recurrences/{recurrence.Id}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await RecurrenceAsync(organizer, recurrence.Id)).CancelledAt.Should().Be(cancelledAt).And.NotBeNull();
            (await DetailsAsync(organizer, first.Id)).Status.Should().Be(GameStatus.Open);
            var cancelled = await DetailsAsync(organizer, recurrence.Occurrences[1].GameId);
            cancelled.Status.Should().Be(GameStatus.Cancelled);
            cancelled.Participants.Single().OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            cancelled.Participants.Single().CancelledAt.Should().BeNull();
            (await UpdateFutureAsync(organizer, recurrence)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task RecurrenceEndpoints_ShouldEnforceOwnershipAuthenticationAndMissingResources()
        {
            using var organizer = await PlayerAsync();
            using var other = await PlayerAsync();
            using var noProfile = await PlayerAsync(withProfile: false);
            using var anonymous = _factory.CreateClient();
            var sourceId = await SourceAsync(organizer);
            var recurrence = await CreateAsync(organizer, sourceId);
            var request = new { Id = Guid.NewGuid(), SourceGameId = sourceId, FirstStartsAt = DateTimeOffset.UtcNow.AddDays(7), OccurrenceCount = 2 };
            (await anonymous.PostAsApiJsonAsync("/api/game-recurrences", request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await noProfile.PostAsApiJsonAsync("/api/game-recurrences", request)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await other.PostAsApiJsonAsync("/api/game-recurrences", request)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await other.GetAsync($"/api/game-recurrences/{recurrence.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await UpdateFutureAsync(other, recurrence)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await other.PostAsync($"/api/game-recurrences/{recurrence.Id}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await anonymous.GetAsync($"/api/game-recurrences/{recurrence.Id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await organizer.GetAsync($"/api/game-recurrences/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await organizer.GetAsync($"/api/game-recurrences/{Guid.Empty}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData(1, 7)]
        [InlineData(53, 7)]
        [InlineData(3, -1)]
        public async Task Create_ShouldValidateBoundariesWithProblemDetails(int count, int days)
        {
            using var organizer = await PlayerAsync();
            var response = await organizer.PostAsApiJsonAsync("/api/game-recurrences", new
            {
                Id = Guid.NewGuid(), SourceGameId = Guid.NewGuid(), FirstStartsAt = DateTimeOffset.UtcNow.AddDays(days), OccurrenceCount = count
            });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        [Fact]
        public async Task FutureEdit_ShouldRejectInvalidRangeAndMissingCourt()
        {
            using var organizer = await PlayerAsync();
            var recurrence = await CreateAsync(organizer, await SourceAsync(organizer));
            (await UpdateFutureAsync(organizer, recurrence, from: 0)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await UpdateFutureAsync(organizer, recurrence, from: 4)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await UpdateFutureAsync(organizer, recurrence with { CourtId = Guid.NewGuid() })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private async Task<HttpClient> PlayerAsync(bool withProfile = true)
        {
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"recurrence-{Guid.NewGuid():N}@test.com", "Unused test password hash");
            context.Users.Add(user);
            if (withProfile) context.PlayerProfiles.Add(PlayerProfile.Create(user.Id, "Recurring Player", PlayerSkillLevel.Intermediate, "Minsk", null));
            await context.SaveChangesAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user));
            return client;
        }

        private static async Task<Guid> SourceAsync(HttpClient organizer, int maxPlayers = 12, GameJoinPolicy policy = GameJoinPolicy.Open)
        {
            var court = await organizer.PostAsApiJsonAsync("/api/courts", new
            {
                Name = "Recurring Court", Address = "Test Street", Latitude = 53.9, Longitude = 27.56, SurfaceType = CourtSurfaceType.Indoor, IsIndoor = true
            });
            court.EnsureSuccessStatusCode();
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var response = await organizer.PostAsApiJsonAsync("/api/games", new
            {
                CourtId = await court.Content.ReadFromApiJsonAsync<Guid>(), StartsAt = startsAt, EndsAt = startsAt.AddHours(2),
                MaxPlayers = maxPlayers, PricePerPlayer = 15, RequiredLevel = GameLevel.Intermediate, JoinPolicy = policy, Description = "Weekly game"
            });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<GameRecurrenceDto> CreateAsync(HttpClient organizer, Guid sourceId, int count = 3)
        {
            var id = Guid.NewGuid();
            var response = await organizer.PostAsApiJsonAsync("/api/game-recurrences", new
            {
                Id = id, SourceGameId = sourceId, FirstStartsAt = DateTimeOffset.UtcNow.AddDays(8), OccurrenceCount = count
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await RecurrenceAsync(organizer, id);
        }

        private static async Task<GameRecurrenceDto> RecurrenceAsync(HttpClient client, Guid id)
        {
            var response = await client.GetAsync($"/api/game-recurrences/{id}");
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromApiJsonAsync<GameRecurrenceDto>())!;
        }

        private static async Task<GameDetailsDto> DetailsAsync(HttpClient client, Guid id)
        {
            var response = await client.GetAsync($"/api/games/{id}");
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromApiJsonAsync<GameDetailsDto>())!;
        }

        private static Task<HttpResponseMessage> UpdateFutureAsync(HttpClient client, GameRecurrenceDto recurrence, int from = 1, decimal price = 15) =>
            client.PutAsApiJsonAsync($"/api/game-recurrences/{recurrence.Id}/future", new
            {
                FromOccurrenceNumber = from, recurrence.CourtId, recurrence.MaxPlayers, PricePerPlayer = price,
                recurrence.RequiredLevel, recurrence.JoinPolicy, Description = "New series settings"
            });

        private static async Task<Guid> JoinAsync(HttpClient player, Guid gameId, bool waitlist = false)
        {
            var response = await player.PostAsync($"/api/games/{gameId}/{(waitlist ? "waitlist" : "participants")}", null);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }
    }
}
