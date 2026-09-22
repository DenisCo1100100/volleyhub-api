using FluentAssertions;
using VolleyHub.Domain.Games;

namespace VolleyHub.Domain.UnitTests.Games
{
    public sealed class GameTemplateTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(120)]
        public void CreateGame_ShouldCopySettingsAndNormalizeScheduleWithoutRecurrence(int? minutes)
        {
            var template = Create(duration: minutes is { } value ? TimeSpan.FromMinutes(value) : null);
            var startsAt = new DateTimeOffset(2027, 1, 10, 20, 0, 0, TimeSpan.FromHours(3));

            var game = template.CreateGame(startsAt);

            game.Id.Should().NotBeEmpty().And.NotBe(template.Id);
            game.OrganizerId.Should().Be(template.OrganizerId);
            game.CourtId.Should().Be(template.CourtId);
            game.StartsAt.Should().Be(startsAt);
            game.StartsAt.Offset.Should().Be(TimeSpan.Zero);
            game.EndsAt.Should().Be(minutes is { } duration ? startsAt.AddMinutes(duration) : null);
            game.MaxPlayers.Should().Be(template.MaxPlayers);
            game.PricePerPlayer.Should().Be(template.PricePerPlayer);
            game.RequiredLevel.Should().Be(template.RequiredLevel);
            game.JoinPolicy.Should().Be(template.JoinPolicy);
            game.Description.Should().Be("Evening game");
            game.Status.Should().Be(GameStatus.Open);
            game.RecurrenceId.Should().BeNull();
            game.OccurrenceNumber.Should().BeNull();
        }

        [Fact]
        public void Update_ShouldChangeOnlyFutureCopiesAndKeepIdentityAndOwnership()
        {
            var template = Create(TimeSpan.FromHours(2));
            var originalId = template.Id;
            var organizerId = template.OrganizerId;
            var startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var first = template.CreateGame(startsAt);
            var newCourtId = Guid.NewGuid();

            template.Update("  Free practice  ", newCourtId, null, 6, 0, GameLevel.Any, GameJoinPolicy.InviteOnly, "  ");
            var second = template.CreateGame(startsAt.AddDays(3));

            template.Id.Should().Be(originalId);
            template.OrganizerId.Should().Be(organizerId);
            template.Name.Should().Be("Free practice");
            first.Id.Should().NotBe(second.Id);
            first.CourtId.Should().NotBe(newCourtId);
            first.EndsAt.Should().Be(startsAt.AddHours(2));
            first.MaxPlayers.Should().Be(12);
            first.PricePerPlayer.Should().Be(25);
            first.RequiredLevel.Should().Be(GameLevel.Intermediate);
            first.JoinPolicy.Should().Be(GameJoinPolicy.ApprovalRequired);
            first.Description.Should().Be("Evening game");
            second.CourtId.Should().Be(newCourtId);
            second.EndsAt.Should().BeNull();
            second.MaxPlayers.Should().Be(6);
            second.PricePerPlayer.Should().Be(0);
            second.RequiredLevel.Should().Be(GameLevel.Any);
            second.JoinPolicy.Should().Be(GameJoinPolicy.InviteOnly);
            second.Description.Should().BeNull();

            second.Cancel();
            template.CreateGame(startsAt.AddDays(5)).Status.Should().Be(GameStatus.Open);
            first.Status.Should().Be(GameStatus.Open);
        }

        [Fact]
        public void Create_ShouldRequireOrganizer()
        {
            var act = () => GameTemplate.Create(Guid.Empty, "Template", Guid.NewGuid(), null, 12, 0, GameLevel.Any, GameJoinPolicy.Open, null);
            act.Should().Throw<ArgumentException>().WithParameterName("organizerId");
        }

        [Theory]
        [InlineData("name-null")]
        [InlineData("name-empty")]
        [InlineData("name-long")]
        [InlineData("court")]
        [InlineData("duration-zero")]
        [InlineData("duration-negative")]
        [InlineData("capacity-low")]
        [InlineData("capacity-high")]
        [InlineData("price")]
        [InlineData("level-unknown")]
        [InlineData("level-undefined")]
        [InlineData("policy-unknown")]
        [InlineData("policy-undefined")]
        [InlineData("description")]
        public void CreateAndUpdate_ShouldRejectInvalidSettingsWithoutChangingTemplate(string invalid)
        {
            var template = Create(TimeSpan.FromHours(2));
            var name = invalid switch { "name-null" => null!, "name-empty" => " ", "name-long" => new string('x', 101), _ => "New name" };
            var courtId = invalid == "court" ? Guid.Empty : Guid.NewGuid();
            var duration = invalid switch { "duration-zero" => TimeSpan.Zero, "duration-negative" => TimeSpan.FromMinutes(-1), _ => TimeSpan.FromHours(1) };
            var capacity = invalid switch { "capacity-low" => Game.MinPlayers - 1, "capacity-high" => Game.MaxPlayersLimit + 1, _ => 6 };
            var price = invalid == "price" ? -1 : 0;
            var level = invalid switch { "level-unknown" => GameLevel.Unknown, "level-undefined" => (GameLevel)999, _ => GameLevel.Any };
            var policy = invalid switch { "policy-unknown" => GameJoinPolicy.Unknown, "policy-undefined" => (GameJoinPolicy)999, _ => GameJoinPolicy.Open };
            var description = invalid == "description" ? new string('x', Game.MaxDescriptionLength + 1) : null;
            var create = () => GameTemplate.Create(template.OrganizerId, name, courtId, duration, capacity, price, level, policy, description);
            var update = () => template.Update(name, courtId, duration, capacity, price, level, policy, description);

            create.Should().Throw<ArgumentException>();
            update.Should().Throw<ArgumentException>();
            template.Name.Should().Be("Practice");
            template.Duration.Should().Be(TimeSpan.FromHours(2));
            template.MaxPlayers.Should().Be(12);
            template.PricePerPlayer.Should().Be(25);
            template.Description.Should().Be("Evening game");
        }

        [Fact]
        public void CreateGame_ShouldRejectMissingStartAndEndDateOverflow()
        {
            var template = Create(TimeSpan.FromHours(2));
            var missing = () => template.CreateGame(default);
            var overflow = () => template.CreateGame(DateTimeOffset.MaxValue);
            missing.Should().Throw<ArgumentException>();
            overflow.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Create_ShouldAcceptGameBoundariesAndNormalizeText()
        {
            var template = GameTemplate.Create(Guid.NewGuid(), new string('x', GameTemplate.MaxNameLength), Guid.NewGuid(),
                TimeSpan.FromMinutes(1), Game.MinPlayers, 0, GameLevel.Any, GameJoinPolicy.Open, new string('d', Game.MaxDescriptionLength));
            template.MaxPlayers.Should().Be(Game.MinPlayers);
            template.Update(" Practice ", template.CourtId, null, Game.MaxPlayersLimit, 10, GameLevel.Any, GameJoinPolicy.Open, "  ");
            template.Name.Should().Be("Practice");
            template.Description.Should().BeNull();
        }

        private static GameTemplate Create(TimeSpan? duration) => GameTemplate.Create(Guid.NewGuid(), " Practice ", Guid.NewGuid(),
            duration, 12, 25, GameLevel.Intermediate, GameJoinPolicy.ApprovalRequired, " Evening game ");
    }
}
