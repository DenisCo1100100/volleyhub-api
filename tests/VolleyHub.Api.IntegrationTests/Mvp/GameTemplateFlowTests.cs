using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VolleyHub.Api.Controllers;
using VolleyHub.Api.IntegrationTests.Common;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.GameTemplates.Common;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Users;
using VolleyHub.Infrastructure.Persistence;

namespace VolleyHub.Api.IntegrationTests.Mvp
{
    public sealed class GameTemplateFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GameTemplateFlowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CrudAndUse_ShouldKeepGamesIndependentAndListOnlyOwnedTemplates()
        {
            using var owner = await PlayerAsync();
            using var other = await PlayerAsync();
            var request = Request(await CourtAsync(owner));
            var template = await CreateAsync(owner, request);
            await CreateAsync(other, request with { Name = "Other player's template" });
            await CreateAsync(owner, request with { Name = "A practice" });
            var list = await ListAsync(owner);
            list.Select(item => item.Name).Should().Equal("A practice", "Practice");
            template.Duration.Should().Be(TimeSpan.FromHours(2));
            template.Description.Should().Be("Evening game");
            template.CreatedAt.Should().NotBe(default);
            template.UpdatedAt.Should().BeNull();

            var startsAt = DateTimeOffset.UtcNow.AddDays(1).ToOffset(TimeSpan.FromHours(3));
            var first = await UseAsync(owner, template.Id, startsAt);
            first.Court.Id.Should().Be(request.CourtId);
            first.StartsAt.Should().Be(startsAt);
            first.StartsAt.Offset.Should().Be(TimeSpan.Zero);
            first.EndsAt.Should().Be(startsAt.AddHours(2));
            first.MaxPlayers.Should().Be(12);
            first.PricePerPlayer.Should().Be(25);
            first.RequiredLevel.Should().Be(GameLevel.Intermediate);
            first.JoinPolicy.Should().Be(GameJoinPolicy.ApprovalRequired);
            first.Status.Should().Be(GameStatus.Open);
            first.RecurrenceId.Should().BeNull();
            first.OccurrenceNumber.Should().BeNull();
            first.Participants.Should().BeEmpty();

            var updated = request with { Name = "Free practice", CourtId = await CourtAsync(owner), Duration = null,
                MaxPlayers = 6, PricePerPlayer = 0, RequiredLevel = GameLevel.Any, JoinPolicy = GameJoinPolicy.Open, Description = null };
            (await owner.PutAsApiJsonAsync($"/api/game-templates/{template.Id}", updated)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await ReadAsync(owner, template.Id)).UpdatedAt.Should().NotBeNull();
            var second = await UseAsync(owner, template.Id, startsAt.AddDays(3));
            second.Id.Should().NotBe(first.Id);
            second.Court.Id.Should().Be(updated.CourtId);
            second.EndsAt.Should().BeNull();
            second.MaxPlayers.Should().Be(6);
            second.PricePerPlayer.Should().Be(0);
            second.RequiredLevel.Should().Be(GameLevel.Any);
            second.JoinPolicy.Should().Be(GameJoinPolicy.Open);
            second.Description.Should().BeNull();

            (await owner.DeleteAsync($"/api/game-templates/{template.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await owner.GetAsync($"/api/game-templates/{template.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await owner.PostAsApiJsonAsync($"/api/game-templates/{template.Id}/games", new { StartsAt = startsAt })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ListAsync(owner)).Should().ContainSingle().Which.Name.Should().Be("A practice");
            (await DetailsAsync(owner, first.Id)).Should().BeEquivalentTo(first);
            (await DetailsAsync(owner, second.Id)).Should().BeEquivalentTo(second);
            var history = await owner.GetAsync("/api/player-profiles/me/organized-games");
            history.EnsureSuccessStatusCode();
            (await history.Content.ReadFromApiJsonAsync<PagedResult<OrganizedGameHistoryDto>>())!.Items.Select(item => item.Game.Id)
                .Should().BeEquivalentTo(new[] { first.Id, second.Id });
        }

        [Fact]
        public async Task GeneratedGame_ShouldKeepParticipationWaitlistPaymentsAttendanceAndHistoryAfterTemplateDeletion()
        {
            using var owner = await PlayerAsync();
            using var firstPlayer = await PlayerAsync();
            using var secondPlayer = await PlayerAsync();
            using var queuedPlayer = await PlayerAsync();
            var request = Request(await CourtAsync(owner)) with { MaxPlayers = 2 };
            var template = await CreateAsync(owner, request);
            var game = await UseAsync(owner, template.Id, DateTimeOffset.UtcNow.AddDays(2));
            var firstId = await JoinAsync(firstPlayer, game.Id);
            var secondId = await JoinAsync(secondPlayer, game.Id);
            (await DetailsAsync(owner, game.Id)).PendingParticipantCount.Should().Be(2);
            (await owner.PostAsync($"/api/game-participants/{firstId}/approve", null)).EnsureSuccessStatusCode();
            (await owner.PostAsync($"/api/game-participants/{secondId}/approve", null)).EnsureSuccessStatusCode();
            var queuedId = await JoinAsync(queuedPlayer, game.Id, waitlist: true);
            (await firstPlayer.PostAsync($"/api/games/{game.Id}/participants/leave", null)).EnsureSuccessStatusCode();
            (await owner.PostAsync($"/api/games/{game.Id}/waitlist/promote", null)).EnsureSuccessStatusCode();
            (await owner.PutAsApiJsonAsync($"/api/game-participants/{queuedId}/offline-payment",
                new { OfflinePaymentStatus = GameParticipantOfflinePaymentStatus.Paid })).EnsureSuccessStatusCode();

            (await owner.PutAsApiJsonAsync($"/api/game-templates/{template.Id}", request with { PricePerPlayer = 0 })).EnsureSuccessStatusCode();
            (await owner.DeleteAsync($"/api/game-templates/{template.Id}")).EnsureSuccessStatusCode();
            (await owner.PostAsync($"/api/games/{game.Id}/complete", null)).EnsureSuccessStatusCode();
            (await owner.PostAsApiJsonAsync($"/api/game-participants/{queuedId}/attendance",
                new { AttendanceStatus = GameParticipantAttendanceStatus.Present })).EnsureSuccessStatusCode();

            var completed = await DetailsAsync(owner, game.Id);
            completed.Status.Should().Be(GameStatus.Completed);
            completed.PricePerPlayer.Should().Be(25);
            completed.Participants.Single(item => item.Id == firstId).CancellationType.Should().Be(GameParticipantCancellationType.OnTime);
            completed.Participants.Single(item => item.Id == queuedId).AttendanceStatus.Should().Be(GameParticipantAttendanceStatus.Present);
            completed.Participants.Single(item => item.Id == queuedId).OfflinePaymentStatus.Should().Be(GameParticipantOfflinePaymentStatus.Paid);
            var history = await queuedPlayer.GetAsync("/api/player-profiles/me/games");
            history.EnsureSuccessStatusCode();
            (await history.Content.ReadFromApiJsonAsync<PagedResult<PlayerGameHistoryDto>>())!.Items
                .Should().ContainSingle(item => item.Game.Id == game.Id && item.Game.RecurrenceId == null);
        }

        [Theory]
        [InlineData("read")]
        [InlineData("update")]
        [InlineData("delete")]
        [InlineData("use")]
        public async Task PrivateEndpoints_ShouldRejectOtherOwnersAndMissingTemplates(string operation)
        {
            using var owner = await PlayerAsync();
            using var other = await PlayerAsync();
            var request = Request(await CourtAsync(owner));
            var template = await CreateAsync(owner, request);
            AssertProblem(await ExecuteAsync(other, operation, template.Id, request), HttpStatusCode.Forbidden);
            AssertProblem(await ExecuteAsync(owner, operation, Guid.NewGuid(), request), HttpStatusCode.NotFound);
            AssertProblem(await ExecuteAsync(owner, operation, Guid.Empty, request), HttpStatusCode.BadRequest);
            (await ReadAsync(owner, template.Id)).Should().BeEquivalentTo(template);
            (await ListAsync(other)).Should().BeEmpty();
        }

        [Theory]
        [InlineData("create")]
        [InlineData("read")]
        [InlineData("list")]
        [InlineData("update")]
        [InlineData("delete")]
        [InlineData("use")]
        public async Task AllEndpoints_ShouldRequireAuthenticationAndActiveProfile(string operation)
        {
            using var owner = await PlayerAsync();
            using var anonymous = _factory.CreateClient();
            using var withoutProfile = await PlayerAsync(withProfile: false);
            using var deletedProfile = await PlayerAsync(deleted: true);
            var request = Request(await CourtAsync(owner));
            var template = await CreateAsync(owner, request);

            (await ExecuteAsync(anonymous, operation, template.Id, request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            AssertProblem(await ExecuteAsync(withoutProfile, operation, template.Id, request), HttpStatusCode.NotFound);
            AssertProblem(await ExecuteAsync(deletedProfile, operation, template.Id, request), HttpStatusCode.NotFound);
            (await ReadAsync(owner, template.Id)).Should().BeEquivalentTo(template);
        }

        [Fact]
        public async Task DeletedCourt_ShouldPreventCreationAndUseButAllowReadingRepairAndDeletion()
        {
            using var owner = await PlayerAsync();
            var request = Request(await CourtAsync(owner));
            var template = await CreateAsync(owner, request);
            var toDelete = await CreateAsync(owner, request);
            (await owner.DeleteAsync($"/api/courts/{request.CourtId}")).EnsureSuccessStatusCode();

            AssertProblem(await owner.PostAsApiJsonAsync("/api/game-templates", request), HttpStatusCode.NotFound);
            AssertProblem(await owner.PostAsApiJsonAsync("/api/game-templates", request with { CourtId = Guid.NewGuid() }), HttpStatusCode.NotFound);
            AssertProblem(await ExecuteAsync(owner, "use", template.Id, request), HttpStatusCode.NotFound);
            AssertProblem(await ExecuteAsync(owner, "update", template.Id, request), HttpStatusCode.NotFound);
            (await ReadAsync(owner, template.Id)).CourtId.Should().Be(request.CourtId);
            (await ListAsync(owner)).Should().HaveCount(2);
            (await owner.DeleteAsync($"/api/game-templates/{toDelete.Id}")).EnsureSuccessStatusCode();
            var repaired = request with { CourtId = await CourtAsync(owner) };
            (await ExecuteAsync(owner, "update", template.Id, repaired)).EnsureSuccessStatusCode();
            (await UseAsync(owner, template.Id, DateTimeOffset.UtcNow.AddDays(1))).Court.Id.Should().Be(repaired.CourtId);
        }

        [Theory]
        [InlineData("name", "")]
        [InlineData("name", " ")]
        [InlineData("name", null)]
        [InlineData("duration", "00:00:00")]
        [InlineData("duration", "-00:01:00")]
        [InlineData("duration", "invalid")]
        [InlineData("courtId", "00000000-0000-0000-0000-000000000000")]
        [InlineData("maxPlayers", 1)]
        [InlineData("maxPlayers", 25)]
        [InlineData("pricePerPlayer", -1)]
        [InlineData("requiredLevel", "Unknown")]
        [InlineData("requiredLevel", "Invalid")]
        [InlineData("joinPolicy", "Unknown")]
        [InlineData("joinPolicy", "Invalid")]
        public async Task CreateAndUpdate_ShouldValidateInputWithProblemDetails(string property, object? value)
        {
            using var owner = await PlayerAsync();
            var request = Request(await CourtAsync(owner));
            var template = await CreateAsync(owner, request);
            var payload = Payload(request);
            payload[property] = value;
            AssertProblem(await owner.PostAsApiJsonAsync("/api/game-templates", payload), HttpStatusCode.BadRequest);
            AssertProblem(await owner.PutAsApiJsonAsync($"/api/game-templates/{template.Id}", payload), HttpStatusCode.BadRequest);
            (await ReadAsync(owner, template.Id)).Should().BeEquivalentTo(template);
        }

        [Theory]
        [InlineData("name", 101)]
        [InlineData("description", 2001)]
        public async Task CreateAndUpdate_ShouldRejectExcessiveText(string property, int length)
        {
            await CreateAndUpdate_ShouldValidateInputWithProblemDetails(property, new string('x', length));
        }

        [Fact]
        public async Task Use_ShouldRequireScheduleAndRejectOverflowWithoutPersistingGame()
        {
            using var owner = await PlayerAsync();
            var template = await CreateAsync(owner, Request(await CourtAsync(owner)));
            AssertProblem(await owner.PostAsApiJsonAsync($"/api/game-templates/{template.Id}/games", new { }), HttpStatusCode.BadRequest);
            AssertProblem(await owner.PostAsApiJsonAsync($"/api/game-templates/{template.Id}/games", new { StartsAt = DateTimeOffset.MaxValue }), HttpStatusCode.BadRequest);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var organizerId = (await db.GameTemplates.SingleAsync(item => item.Id == template.Id)).OrganizerId;
            (await db.Games.AnyAsync(game => game.OrganizerId == organizerId)).Should().BeFalse();
        }

        private async Task<HttpClient> PlayerAsync(bool withProfile = true, bool deleted = false)
        {
            var client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = User.Create($"template-{Guid.NewGuid():N}@test.com", "Unused test password hash");
            db.Users.Add(user);
            if (withProfile)
            {
                var profile = PlayerProfile.Create(user.Id, "Template Player", PlayerSkillLevel.Intermediate, "Minsk", null);
                if (deleted) profile.Delete();
                db.PlayerProfiles.Add(profile);
            }
            await db.SaveChangesAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateToken(user));
            return client;
        }

        private static CreateGameTemplateRequest Request(Guid courtId) => new("Practice", courtId, TimeSpan.FromHours(2),
            12, 25, GameLevel.Intermediate, GameJoinPolicy.ApprovalRequired, "Evening game");

        private static Dictionary<string, object?> Payload(CreateGameTemplateRequest request) => new()
        {
            ["name"] = request.Name, ["courtId"] = request.CourtId, ["duration"] = request.Duration,
            ["maxPlayers"] = request.MaxPlayers, ["pricePerPlayer"] = request.PricePerPlayer,
            ["requiredLevel"] = request.RequiredLevel, ["joinPolicy"] = request.JoinPolicy, ["description"] = request.Description
        };

        private static async Task<Guid> CourtAsync(HttpClient owner)
        {
            var response = await owner.PostAsApiJsonAsync("/api/courts", new
            {
                Name = "Template Court", Address = "Test Street", Latitude = 53.9, Longitude = 27.56,
                SurfaceType = CourtSurfaceType.Indoor, IsIndoor = true
            });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static async Task<GameTemplateDto> CreateAsync(HttpClient owner, CreateGameTemplateRequest request)
        {
            var response = await owner.PostAsApiJsonAsync("/api/game-templates", request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var id = await response.Content.ReadFromApiJsonAsync<Guid>();
            response.Headers.Location!.AbsolutePath.Should().Be($"/api/game-templates/{id}");
            return await ReadAsync(owner, id);
        }

        private static async Task<GameTemplateDto> ReadAsync(HttpClient owner, Guid id)
        {
            var response = await owner.GetAsync($"/api/game-templates/{id}");
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromApiJsonAsync<GameTemplateDto>())!;
        }

        private static async Task<IReadOnlyList<GameTemplateDto>> ListAsync(HttpClient owner)
        {
            var response = await owner.GetAsync("/api/game-templates");
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromApiJsonAsync<GameTemplateDto[]>())!;
        }

        private static async Task<GameDetailsDto> UseAsync(HttpClient owner, Guid id, DateTimeOffset startsAt)
        {
            var response = await owner.PostAsApiJsonAsync($"/api/game-templates/{id}/games", new { StartsAt = startsAt });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var gameId = await response.Content.ReadFromApiJsonAsync<Guid>();
            response.Headers.Location!.AbsolutePath.Should().Be($"/api/games/{gameId}");
            return await DetailsAsync(owner, gameId);
        }

        private static async Task<GameDetailsDto> DetailsAsync(HttpClient client, Guid id)
        {
            var response = await client.GetAsync($"/api/games/{id}");
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromApiJsonAsync<GameDetailsDto>())!;
        }

        private static async Task<Guid> JoinAsync(HttpClient player, Guid gameId, bool waitlist = false)
        {
            var response = await player.PostAsync($"/api/games/{gameId}/{(waitlist ? "waitlist" : "participants")}", null);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await response.Content.ReadFromApiJsonAsync<Guid>();
        }

        private static void AssertProblem(HttpResponseMessage response, HttpStatusCode status)
        {
            response.StatusCode.Should().Be(status);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        private static Task<HttpResponseMessage> ExecuteAsync(HttpClient client, string operation, Guid id, CreateGameTemplateRequest request) => operation switch
        {
            "create" => client.PostAsApiJsonAsync("/api/game-templates", request),
            "read" => client.GetAsync($"/api/game-templates/{id}"),
            "list" => client.GetAsync("/api/game-templates"),
            "update" => client.PutAsApiJsonAsync($"/api/game-templates/{id}", request),
            "delete" => client.DeleteAsync($"/api/game-templates/{id}"),
            "use" => client.PostAsApiJsonAsync($"/api/game-templates/{id}/games", new { StartsAt = DateTimeOffset.UtcNow.AddDays(1) }),
            _ => throw new ArgumentException("Unknown operation.", nameof(operation))
        };
    }
}
