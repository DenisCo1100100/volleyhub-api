using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class GameHistoryEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string PlayerHistory = "/api/player-profiles/me/games";
        private const string OrganizedHistory = "/api/player-profiles/me/organized-games";
        private readonly CustomWebApplicationFactory _factory;

        public GameHistoryEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(PlayerHistory)]
        [InlineData(OrganizedHistory)]
        public async Task History_ShouldRequireAuthenticationAndActiveProfile(string endpoint)
        {
            using var anonymous = _factory.CreateClient();
            using var missing = await CreatePlayerAsync(withProfile: false);
            using var deleted = await CreatePlayerAsync(deleted: true);
            (await anonymous.GetAsync(endpoint)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            foreach (var client in new[] { missing.Client, deleted.Client })
            {
                var response = await client.GetAsync(endpoint);
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            }
        }

        [Theory]
        [InlineData(PlayerHistory)]
        [InlineData(OrganizedHistory)]
        public async Task History_ShouldReturnEmptyPageAndValidateFilters(string endpoint)
        {
            using var player = await CreatePlayerAsync();
            var empty = await ReadPageAsync<JsonElement>(player.Client, endpoint);
            empty.Items.Should().BeEmpty();
            empty.Page.Should().Be(1);
            empty.PageSize.Should().Be(20);
            empty.TotalCount.Should().Be(0);
            empty.TotalPages.Should().Be(0);

            foreach (var filter in new[] { "page=0", "pageSize=101", "page=2147483647&pageSize=100", "period=999",
                "period=Invalid", "status=999", "courtId=00000000-0000-0000-0000-000000000000",
                "startsAtFrom=2030-01-02T00:00:00Z&startsAtTo=2030-01-01T00:00:00Z" })
            {
                var response = await player.Client.GetAsync($"{endpoint}?{filter}");
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest, filter);
                response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            }

            if (endpoint == PlayerHistory)
            {
                (await player.Client.GetAsync($"{endpoint}?joinStatus=Unknown")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
                (await player.Client.GetAsync($"{endpoint}?joinStatus=999")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
        }

        [Fact]
        public async Task PlayerHistory_ShouldIncludeAllParticipationStatesAndOnlyOwnRecords()
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            using var outsider = await CreatePlayerAsync();
            var startsAt = DateTimeOffset.UtcNow.AddDays(3);
            var expected = new List<GameParticipant>();
            Guid courtId = default;
            await SeedAsync(context =>
            {
                var court = CreateCourt(organizer.Profile.Id);
                courtId = court.Id;
                context.Courts.Add(court);
                foreach (var status in Enum.GetValues<GameParticipantJoinStatus>().Where(status => status != GameParticipantJoinStatus.Unknown))
                {
                    var game = CreateGame(organizer.Profile.Id, court.Id, startsAt);
                    var participant = CreateParticipant(game, player.Profile.Id, status);
                    if (status == GameParticipantJoinStatus.Approved)
                    {
                        participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);
                        game.Complete();
                        participant.MarkAttendance(GameParticipantAttendanceStatus.Present);
                    }
                    context.AddRange(game, participant);
                    expected.Add(participant);
                }
                var unrelatedGame = CreateGame(organizer.Profile.Id, court.Id, startsAt);
                context.AddRange(unrelatedGame, CreateParticipant(unrelatedGame, outsider.Profile.Id, GameParticipantJoinStatus.Approved));
                context.Games.Add(CreateGame(player.Profile.Id, court.Id, startsAt));
            });

            var page = await ReadPageAsync<PlayerGameHistoryDto>(player.Client, PlayerHistory);
            page.TotalCount.Should().Be(6);
            page.Items.Select(item => item.Participation.Id).Should().BeEquivalentTo(expected.Select(p => p.Id));
            foreach (var item in page.Items)
            {
                var participant = expected.Single(p => p.Id == item.Participation.Id);
                item.Game.Court.Id.Should().Be(courtId);
                item.Game.Court.Name.Should().Be("History Court");
                item.Game.Organizer.Id.Should().Be(organizer.Profile.Id);
                item.Game.StartsAt.Should().Be(startsAt);
                item.Game.EndsAt.Should().Be(startsAt.AddHours(2));
                item.Game.CurrentUserJoinStatus.Should().Be(participant.JoinStatus);
                item.Participation.Should().BeEquivalentTo(new GameParticipationHistoryDto(participant.Id, participant.JoinStatus,
                    participant.AttendanceStatus, participant.OfflinePaymentStatus, participant.JoinedAt, participant.ApprovedAt,
                    participant.CancelledAt, participant.RemovedAt, participant.CancellationType));
            }

            var completed = page.Items.Single(item => item.Game.Status == GameStatus.Completed);
            completed.Participation.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
            completed.Participation.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            var cancelled = await ReadPageAsync<PlayerGameHistoryDto>(player.Client, $"{PlayerHistory}?joinStatus=Cancelled");
            cancelled.TotalCount.Should().Be(1);
            cancelled.Items.Single().Participation.CancellationType.Should().Be(GameParticipantCancellationType.OnTime);
            using var json = JsonDocument.Parse(await (await player.Client.GetAsync(PlayerHistory)).Content.ReadAsStringAsync());
            var first = json.RootElement.GetProperty("items")[0];
            first.GetProperty("game").GetProperty("status").ValueKind.Should().Be(JsonValueKind.String);
            foreach (var property in new[] { "joinStatus", "attendanceStatus", "offlinePaymentStatus" })
            {
                first.GetProperty("participation").GetProperty(property).ValueKind.Should().Be(JsonValueKind.String);
            }
        }

        [Fact]
        public async Task OrganizedHistory_ShouldReturnOperationalCountsAndMatchExistingPaymentSummary()
        {
            using var organizer = await CreatePlayerAsync();
            using var outsider = await CreatePlayerAsync();
            Guid gameId = default;
            await SeedAsync(context =>
            {
                var court = CreateCourt(organizer.Profile.Id);
                var game = CreateGame(organizer.Profile.Id, court.Id, DateTimeOffset.UtcNow.AddDays(-1));
                gameId = game.Id;
                context.AddRange(court, game);
                for (var i = 0; i < 3; i++)
                {
                    var participant = CreateParticipant(game, i == 0 ? organizer.Profile.Id : Guid.NewGuid(), GameParticipantJoinStatus.Approved);
                    if (i == 0) participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);
                    if (i < 2) participant.MarkAttendance(i == 0 ? GameParticipantAttendanceStatus.Present : GameParticipantAttendanceStatus.Absent);
                    context.GameParticipants.Add(participant);
                }
                foreach (var status in new[] { GameParticipantJoinStatus.PendingApproval, GameParticipantJoinStatus.Waitlisted,
                    GameParticipantJoinStatus.Rejected, GameParticipantJoinStatus.Cancelled, GameParticipantJoinStatus.Removed })
                {
                    context.GameParticipants.Add(CreateParticipant(game, Guid.NewGuid(), status));
                }
                game.Complete();
                context.Games.Add(CreateGame(outsider.Profile.Id, court.Id, game.StartsAt));
            });

            var page = await ReadPageAsync<OrganizedGameHistoryDto>(organizer.Client, OrganizedHistory);
            page.TotalCount.Should().Be(1);
            var item = page.Items.Single();
            item.Game.Id.Should().Be(gameId);
            item.Game.CurrentUserJoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            item.Game.ApprovedParticipantCount.Should().Be(3);
            item.Game.PendingParticipantCount.Should().Be(1);
            item.Game.AvailableSpots.Should().Be(9);
            item.WaitlistedParticipantCount.Should().Be(1);
            item.PresentParticipantCount.Should().Be(1);
            item.AbsentParticipantCount.Should().Be(1);
            item.UnmarkedAttendanceParticipantCount.Should().Be(1);
            item.OfflinePayments.ExpectedAmount.Should().Be(37.05m);
            item.OfflinePayments.PaidAmount.Should().Be(12.35m);
            item.OfflinePayments.OutstandingAmount.Should().Be(24.70m);
            var summary = await (await organizer.Client.GetAsync($"/api/games/{gameId}/offline-payments"))
                .Content.ReadFromApiJsonAsync<GameOfflinePaymentSummaryDto>();
            item.OfflinePayments.Should().BeEquivalentTo(summary);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task History_ShouldFilterAndPageWithStableOrdering(bool organized)
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            var startsAt = DateTimeOffset.UtcNow.AddDays(10);
            var games = new List<Game>();
            Guid courtId = default;
            await SeedAsync(context =>
            {
                var court = CreateCourt(organizer.Profile.Id);
                courtId = court.Id;
                context.Courts.Add(court);
                for (var i = 0; i < 4; i++)
                {
                    var game = CreateGame(organizer.Profile.Id, court.Id, startsAt);
                    var participant = CreateParticipant(game, player.Profile.Id, GameParticipantJoinStatus.Approved);
                    if (i == 2) game.Cancel();
                    if (i == 3) game.Complete();
                    context.AddRange(game, participant);
                    games.Add(game);
                }
                foreach (var time in new[] { startsAt.AddDays(1), DateTimeOffset.UtcNow.AddDays(-1) })
                {
                    var game = CreateGame(organizer.Profile.Id, court.Id, time);
                    context.AddRange(game, CreateParticipant(game, player.Profile.Id, GameParticipantJoinStatus.Approved));
                    games.Add(game);
                }
                var otherCourt = CreateCourt(organizer.Profile.Id);
                var otherGame = CreateGame(organizer.Profile.Id, otherCourt.Id, startsAt);
                context.AddRange(otherCourt, otherGame, CreateParticipant(otherGame, player.Profile.Id, GameParticipantJoinStatus.Approved));
            });

            var client = organized ? organizer.Client : player.Client;
            var endpoint = organized ? OrganizedHistory : PlayerHistory;
            var date = Uri.EscapeDataString(startsAt.ToOffset(TimeSpan.FromHours(3)).ToString("O"));
            var filter = $"courtId={courtId}&startsAtFrom={date}&startsAtTo={date}&status=Open";
            var expected = games.Take(2).OrderByDescending(game => game.Id).Select(game => game.Id).ToArray();
            for (var page = 1; page <= 2; page++)
            {
                var result = await ReadPageAsync<JsonElement>(client, $"{endpoint}?{filter}&page={page}&pageSize=1");
                result.TotalCount.Should().Be(2);
                result.TotalPages.Should().Be(2);
                result.Page.Should().Be(page);
                result.Items.Single().GetProperty("game").GetProperty("id").GetGuid().Should().Be(expected[page - 1]);
            }
            var beyond = await ReadPageAsync<JsonElement>(client, $"{endpoint}?{filter}&page=3&pageSize=1");
            beyond.Items.Should().BeEmpty();
            beyond.TotalCount.Should().Be(2);
            var upcoming = await ReadPageAsync<JsonElement>(client, $"{endpoint}?courtId={courtId}&period=Upcoming");
            upcoming.Items.Select(GameId).Should().Equal(games.Where(g => g.Status == GameStatus.Open && g.StartsAt >= startsAt)
                .OrderBy(g => g.StartsAt).ThenBy(g => g.Id).Select(g => g.Id));
            var past = await ReadPageAsync<JsonElement>(client, $"{endpoint}?courtId={courtId}&period=Past");
            past.Items.Select(GameId).Should().Equal(games.Last().Id);
            foreach (var status in new[] { GameStatus.Completed, GameStatus.Cancelled })
            {
                var result = await ReadPageAsync<JsonElement>(client, $"{endpoint}?status={status}");
                result.Items.Select(GameId).Should().Equal(games.Single(g => g.Status == status).Id);
            }
        }

        [Fact]
        public async Task History_ShouldRetainSoftDeletedRelationshipsAndRecordedPaymentsAfterGameCancellation()
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            Guid gameId = default;
            Guid courtId = default;
            await SeedAsync(context =>
            {
                var court = CreateCourt(organizer.Profile.Id);
                var game = CreateGame(organizer.Profile.Id, court.Id, DateTimeOffset.UtcNow.AddDays(2));
                var participant = CreateParticipant(game, player.Profile.Id, GameParticipantJoinStatus.Approved);
                participant.UpdateOfflinePaymentStatus(game, GameParticipantOfflinePaymentStatus.Paid);
                courtId = court.Id;
                gameId = game.Id;
                context.AddRange(court, game, participant);
            });
            (await organizer.Client.PostAsync($"/api/games/{gameId}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            var organized = (await ReadPageAsync<OrganizedGameHistoryDto>(organizer.Client, OrganizedHistory)).Items.Single();
            organized.OfflinePayments.ExpectedParticipantCount.Should().Be(0);
            organized.OfflinePayments.PaidParticipantCount.Should().Be(0);
            organized.OfflinePayments.OutstandingParticipantCount.Should().Be(0);
            await SeedAsync(context =>
            {
                context.Courts.Single(c => c.Id == courtId).Delete();
                context.PlayerProfiles.Single(p => p.Id == organizer.Profile.Id).Delete();
            });
            var page = await ReadPageAsync<PlayerGameHistoryDto>(player.Client, PlayerHistory);
            page.TotalCount.Should().Be(1);
            page.Items.Single().Game.Id.Should().Be(gameId);
            page.Items.Single().Game.Status.Should().Be(GameStatus.Cancelled);
            page.Items.Single().Participation.JoinStatus.Should().Be(GameParticipantJoinStatus.Approved);
            page.Items.Single().Participation.OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            page.Items.Single().Participation.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.NotMarked);
            (await player.Client.GetAsync($"{OrganizedHistory}?playerProfileId={organizer.Profile.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadPageAsync<OrganizedGameHistoryDto>(player.Client, OrganizedHistory)).Items.Should().BeEmpty();
            (await _factory.CreateClient().GetAsync("/api/games")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private async Task<TestPlayer> CreatePlayerAsync(bool withProfile = true, bool deleted = false)
        {
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"history-{Guid.NewGuid():N}@test.com", "Unused test password hash");
            var profile = PlayerProfile.Create(user.Id, "History Player", PlayerSkillLevel.Intermediate, "Minsk", null);
            if (deleted) profile.Delete();
            context.Users.Add(user);
            if (withProfile) context.PlayerProfiles.Add(profile);
            await context.SaveChangesAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user));
            return new TestPlayer(client, profile);
        }

        private async Task SeedAsync(Action<ApplicationDbContext> seed)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seed(context);
            await context.SaveChangesAsync();
        }

        private static Court CreateCourt(Guid organizerId) => Court.Create(organizerId, "History Court", "Minsk, Test Street 1",
            53.9, 27.56, CourtSurfaceType.Indoor, true, null);

        private static Game CreateGame(Guid organizerId, Guid courtId, DateTimeOffset startsAt) =>
            Game.Create(organizerId, courtId, startsAt, startsAt.AddHours(2), 12, 12.35m, GameLevel.Any, GameJoinPolicy.Open, null);

        private static GameParticipant CreateParticipant(Game game, Guid profileId, GameParticipantJoinStatus status)
        {
            var joinedAt = game.StartsAt.AddDays(-3);
            if (status == GameParticipantJoinStatus.Waitlisted)
            {
                game.MarkAsFull();
                var waitlisted = GameParticipant.JoinWaitlist(game, profileId, joinedAt, game.MaxPlayers);
                game.Reopen();
                return waitlisted;
            }
            if (status is GameParticipantJoinStatus.PendingApproval or GameParticipantJoinStatus.Rejected)
            {
                var pending = GameParticipant.RequestToJoin(game.Id, profileId, joinedAt, GameParticipantOfflinePaymentStatus.Pending);
                if (status == GameParticipantJoinStatus.Rejected) pending.Reject();
                return pending;
            }
            var participant = GameParticipant.JoinOpenGame(game.Id, profileId, joinedAt, GameParticipantOfflinePaymentStatus.Pending);
            if (status == GameParticipantJoinStatus.Cancelled) participant.CancelParticipation(joinedAt.AddHours(1), game.StartsAt);
            if (status == GameParticipantJoinStatus.Removed) participant.Remove(joinedAt.AddHours(1), game.StartsAt);
            return participant;
        }

        private static Guid GameId(JsonElement item) => item.GetProperty("game").GetProperty("id").GetGuid();

        private static async Task<PagedResult<T>> ReadPageAsync<T>(HttpClient client, string endpoint)
        {
            var response = await client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromApiJsonAsync<PagedResult<T>>())!;
        }

        private sealed record TestPlayer(HttpClient Client, PlayerProfile Profile) : IDisposable
        {
            public void Dispose() => Client.Dispose();
        }
    }
}
