using FluentValidation;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameTemplates.Commands.UpdateGameTemplate
{
    public sealed class UpdateGameTemplateCommandValidator : AbstractValidator<UpdateGameTemplateCommand>
    {
        public UpdateGameTemplateCommandValidator()
        {
            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.Name).NotEmpty().MaximumLength(GameTemplate.MaxNameLength);
            RuleFor(command => command.CourtId).NotEmpty();
            RuleFor(command => command.Duration).GreaterThan(TimeSpan.Zero).When(command => command.Duration is not null);
            RuleFor(command => command.MaxPlayers).InclusiveBetween(Game.MinPlayers, Game.MaxPlayersLimit);
            RuleFor(command => command.PricePerPlayer).GreaterThanOrEqualTo(0);
            RuleFor(command => command.RequiredLevel).Must(level => level is not GameLevel.Unknown && Enum.IsDefined(level))
                .WithMessage("Game level is invalid.");
            RuleFor(command => command.JoinPolicy).Must(policy => policy is not GameJoinPolicy.Unknown && Enum.IsDefined(policy))
                .WithMessage("Game join policy is invalid.");
            RuleFor(command => command.Description).MaximumLength(Game.MaxDescriptionLength);
        }
    }
}
