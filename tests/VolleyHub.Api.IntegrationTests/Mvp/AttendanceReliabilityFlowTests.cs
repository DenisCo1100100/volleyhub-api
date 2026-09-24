using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class AttendanceReliabilityFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AttendanceReliabilityFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Reliability_ShouldSeparateAttendanceAndCancellationsAndReflectOrganizerCorrections()
        {
            using var organizer = await CreatePlayerAsync();
            using var player = await CreatePlayerAsync();
            var courtResponse = await organizer.Client.PostAsApiJsonAsync("/api/courts", new
            {
                Name = "Reliability Court", Address = "Test Street", Latitude = 53.9, Longitude = 27.56,
                SurfaceType = CourtSurfaceType.Indoor, IsIndoor = true
            });
            courtResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var courtId = await courtResponse.Content.ReadFromApiJsonAsync<Guid>();
            var games = new List<(Guid GameId, Guid ParticipantId)>();
            for (var i = 0; i < 5; i++)
            {
                var create = await organizer.Client.PostAsApiJsonAsync("/api/games", new
                {
                    CourtId = courtId, StartsAt = DateTimeOffset.UtcNow.AddHours(i == 4 ? 12 : 72),
                    MaxPlayers = 12, PricePerPlayer = 15, RequiredLevel = GameLevel.Any, JoinPolicy = GameJoinPolicy.Open
                });
                create.StatusCode.Should().Be(HttpStatusCode.Created);
                var gameId = await create.Content.ReadFromApiJsonAsync<Guid>();
                var join = await player.Client.PostAsync($"/api/games/{gameId}/participants", null);
                join.StatusCode.Should().Be(HttpStatusCode.Created);
                games.Add((gameId, await join.Content.ReadFromApiJsonAsync<Guid>()));
                if (i >= 3)
                    (await player.Client.PostAsync($"/api/games/{gameId}/participants/leave", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            (await ReadSummaryAsync(player.ProfileId)).Should().Be(new PlayerReliabilitySummaryDto(player.ProfileId, 0, 0, 0, 0, 0));
            AssertProblem(await MarkAsync(organizer.Client, games[0].ParticipantId, GameParticipantAttendanceStatus.Present), HttpStatusCode.Conflict);
            foreach (var game in games)
                (await organizer.Client.PostAsync($"/api/games/{game.GameId}/complete", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            (await ReadSummaryAsync(player.ProfileId)).Should().Be(new PlayerReliabilitySummaryDto(player.ProfileId, 0, 0, 1, 1, 3));
            (await MarkAsync(organizer.Client, games[0].ParticipantId, GameParticipantAttendanceStatus.Present)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await MarkAsync(organizer.Client, games[1].ParticipantId, GameParticipantAttendanceStatus.Absent)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            foreach (var cancelled in games.Skip(3))
            {
                AssertProblem(await MarkAsync(organizer.Client, cancelled.ParticipantId, GameParticipantAttendanceStatus.Absent), HttpStatusCode.Conflict);
                AssertProblem(await MarkAsync(organizer.Client, cancelled.ParticipantId, GameParticipantAttendanceStatus.Present), HttpStatusCode.Conflict);
            }

            var initial = await ReadSummaryAsync(player.ProfileId);
            initial.Should().Be(new PlayerReliabilitySummaryDto(player.ProfileId, 1, 1, 1, 1, 1));
            initial.AttendanceRate.Should().Be(50);
            using var anonymous = _factory.CreateClient();
            AssertProblem(await MarkAsync(player.Client, games[1].ParticipantId, GameParticipantAttendanceStatus.Present), HttpStatusCode.Forbidden);
            (await MarkAsync(anonymous, games[1].ParticipantId, GameParticipantAttendanceStatus.Present)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            for (var retry = 0; retry < 2; retry++)
                (await MarkAsync(organizer.Client, games[1].ParticipantId, GameParticipantAttendanceStatus.Present)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            var corrected = await ReadSummaryAsync(player.ProfileId);
            corrected.Should().Be(new PlayerReliabilitySummaryDto(player.ProfileId, 2, 0, 1, 1, 1));
            corrected.TotalMarkedGamesCount.Should().Be(2);
            corrected.AttendanceRate.Should().Be(100);
            AssertProblem(await MarkAsync(organizer.Client, games[1].ParticipantId, GameParticipantAttendanceStatus.NotMarked), HttpStatusCode.BadRequest);
            var invalid = await organizer.Client.PostAsApiJsonAsync($"/api/game-participants/{games[1].ParticipantId}/attendance", new { AttendanceStatus = "LateCancel" });
            AssertProblem(invalid, HttpStatusCode.BadRequest);
            (await ReadSummaryAsync(player.ProfileId)).Should().Be(corrected);

            var historyResponse = await player.Client.GetAsync("/api/player-profiles/me/games");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var history = (await historyResponse.Content.ReadFromApiJsonAsync<PagedResult<PlayerGameHistoryDto>>())!;
            history.Items.Single(i => i.Game.Id == games[1].GameId).Participation.AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
            history.Items.Should().OnlyContain(i => i.Participation.OfflinePaymentStatus == GameParticipantOfflinePaymentStatus.Pending);
            var cancelledHistory = history.Items.Where(i => i.Participation.JoinStatus == GameParticipantJoinStatus.Cancelled).ToArray();
            cancelledHistory.Should().HaveCount(2).And.OnlyContain(i => i.Participation.AttendanceStatus == GameParticipantAttendanceStatus.NotMarked);
            cancelledHistory.Select(i => i.Participation.CancellationType).Should().BeEquivalentTo(
                new[] { GameParticipantCancellationType.OnTime, GameParticipantCancellationType.Late });
            var organized = await organizer.Client.GetAsync("/api/player-profiles/me/organized-games");
            var organizedHistory = (await organized.Content.ReadFromApiJsonAsync<PagedResult<OrganizedGameHistoryDto>>())!;
            var correctedGame = organizedHistory.Items.Single(i => i.Game.Id == games[1].GameId);
            correctedGame.PresentParticipantCount.Should().Be(1);
            correctedGame.AbsentParticipantCount.Should().Be(0);

            (await MarkAsync(organizer.Client, games[1].ParticipantId, GameParticipantAttendanceStatus.Absent)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await ReadSummaryAsync(player.ProfileId)).Should().Be(initial);
            (await ReadSummaryAsync(organizer.ProfileId)).Should().Be(new PlayerReliabilitySummaryDto(organizer.ProfileId, 0, 0, 0, 0, 0));

            using var json = JsonDocument.Parse(await anonymous.GetStringAsync($"/api/player-profiles/{player.ProfileId}/reliability"));
            var body = json.RootElement;
            body.GetProperty("playerProfileId").GetGuid().Should().Be(player.ProfileId);
            foreach (var property in new[] { "attendedGamesCount", "noShowCount", "lateCancellationCount", "onTimeCancellationCount", "unmarkedGamesCount" })
                body.GetProperty(property).GetInt32().Should().Be(1);
            body.GetProperty("totalMarkedGamesCount").GetInt32().Should().Be(2);
            body.GetProperty("attendanceRate").GetDecimal().Should().Be(50);
            body.EnumerateObject().Should().HaveCount(8);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Reliability_ShouldReturnNotFoundForMissingOrDeletedProfile(bool deleted)
        {
            using var player = await CreatePlayerAsync(deleted);
            using var client = _factory.CreateClient();
            AssertProblem(await client.GetAsync($"/api/player-profiles/{(deleted ? player.ProfileId : Guid.NewGuid())}/reliability"), HttpStatusCode.NotFound);
        }

        private async Task<PlayerReliabilitySummaryDto> ReadSummaryAsync(Guid profileId)
        {
            using var client = _factory.CreateClient();
            var response = await client.GetAsync($"/api/player-profiles/{profileId}/reliability");
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromApiJsonAsync<PlayerReliabilitySummaryDto>())!;
        }

        private async Task<TestPlayer> CreatePlayerAsync(bool deleted = false)
        {
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"reliability-{Guid.NewGuid():N}@test.com", "Unused test password hash");
            var profile = PlayerProfile.Create(user.Id, "Reliability Player", PlayerSkillLevel.Intermediate, "Minsk", null);
            if (deleted) profile.Delete();
            context.AddRange(user, profile);
            await context.SaveChangesAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user));
            return new TestPlayer(client, profile.Id);
        }

        private static Task<HttpResponseMessage> MarkAsync(HttpClient client, Guid participantId, GameParticipantAttendanceStatus status) =>
            client.PostAsApiJsonAsync($"/api/game-participants/{participantId}/attendance", new { AttendanceStatus = status });

        private static void AssertProblem(HttpResponseMessage response, HttpStatusCode status)
        {
            response.StatusCode.Should().Be(status);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        private sealed record TestPlayer(HttpClient Client, Guid ProfileId) : IDisposable
        {
            public void Dispose() => Client.Dispose();
        }
    }
}
