using FluentValidation;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.UpdateGame
{
    public sealed class UpdateGameCommandValidator : AbstractValidator<UpdateGameCommand>
    {
        public UpdateGameCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty();

            RuleFor(command => command.CourtId)
                .NotEmpty();

            RuleFor(command => command.StartsAt)
                .NotEmpty();

            RuleFor(command => command.MaxPlayers)
                .InclusiveBetween(Game.MinPlayers, Game.MaxPlayersLimit);

            RuleFor(command => command.Description)
                .MaximumLength(Game.MaxDescriptionLength);
        }
    }
}