using FluentValidation;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameRecurrences.Commands.UpdateFutureGames
{
    public sealed class UpdateFutureGamesCommandValidator : AbstractValidator<UpdateFutureGamesCommand>
    {
        public UpdateFutureGamesCommandValidator()
        {
            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.FromOccurrenceNumber).InclusiveBetween(1, GameRecurrence.MaxOccurrences);
            RuleFor(command => command.CourtId).NotEmpty();
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
