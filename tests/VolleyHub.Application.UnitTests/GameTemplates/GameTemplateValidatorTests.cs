using FluentAssertions;
using VolleyHub.Application.GameTemplates.Commands.CreateGameFromTemplate;
using VolleyHub.Application.GameTemplates.Commands.CreateGameTemplate;
using VolleyHub.Application.GameTemplates.Commands.DeleteGameTemplate;
using VolleyHub.Application.GameTemplates.Commands.UpdateGameTemplate;
using VolleyHub.Application.GameTemplates.Queries.GetGameTemplateById;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameTemplates
{
    public sealed class GameTemplateValidatorTests
    {
        [Theory]
        [InlineData(GameLevel.Unknown, GameJoinPolicy.Open)]
        [InlineData((GameLevel)999, GameJoinPolicy.Open)]
        [InlineData(GameLevel.Any, GameJoinPolicy.Unknown)]
        [InlineData(GameLevel.Any, (GameJoinPolicy)999)]
        public void CreateAndUpdate_ShouldRejectUnknownAndUndefinedEnums(GameLevel level, GameJoinPolicy policy)
        {
            var create = new CreateGameTemplateCommand("Practice", Guid.NewGuid(), null, 12, 0, level, policy, null);
            var update = new UpdateGameTemplateCommand(Guid.NewGuid(), create.Name, create.CourtId, create.Duration,
                create.MaxPlayers, create.PricePerPlayer, level, policy, create.Description);
            new CreateGameTemplateCommandValidator().Validate(create).IsValid.Should().BeFalse();
            new UpdateGameTemplateCommandValidator().Validate(update).IsValid.Should().BeFalse();
        }

        [Fact]
        public void CommandsAndQueries_ShouldRequireTemplateIdAndRuntimeStart()
        {
            new UpdateGameTemplateCommandValidator().Validate(new UpdateGameTemplateCommand(Guid.Empty, "Practice", Guid.NewGuid(), null, 12, 0,
                GameLevel.Any, GameJoinPolicy.Open, null)).Errors.Should().ContainSingle(error => error.PropertyName == "Id");
            new DeleteGameTemplateCommandValidator().Validate(new DeleteGameTemplateCommand(Guid.Empty)).IsValid.Should().BeFalse();
            new GetGameTemplateByIdQueryValidator().Validate(new GetGameTemplateByIdQuery(Guid.Empty)).IsValid.Should().BeFalse();
            new CreateGameFromTemplateCommandValidator().Validate(new CreateGameFromTemplateCommand(Guid.Empty, default)).Errors
                .Select(error => error.PropertyName).Should().BeEquivalentTo(new[] { "Id", "StartsAt" });
        }

        [Theory]
        [InlineData(null, Game.MinPlayers)]
        [InlineData(120, Game.MaxPlayersLimit)]
        public void CreateAndUpdate_ShouldAcceptOptionalDurationAndGameBoundaries(int? minutes, int capacity)
        {
            var create = new CreateGameTemplateCommand(new string('n', GameTemplate.MaxNameLength), Guid.NewGuid(),
                minutes is { } value ? TimeSpan.FromMinutes(value) : null, capacity, 0, GameLevel.Any, GameJoinPolicy.Open,
                new string('d', Game.MaxDescriptionLength));
            var update = new UpdateGameTemplateCommand(Guid.NewGuid(), create.Name, create.CourtId, create.Duration,
                create.MaxPlayers, create.PricePerPlayer, create.RequiredLevel, create.JoinPolicy, create.Description);
            new CreateGameTemplateCommandValidator().Validate(create).IsValid.Should().BeTrue();
            new UpdateGameTemplateCommandValidator().Validate(update).IsValid.Should().BeTrue();
        }
    }
}
