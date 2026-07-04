using FluentValidation;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.CreateGame
{
    public sealed class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
    {
        public CreateGameCommandValidator()
        {
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