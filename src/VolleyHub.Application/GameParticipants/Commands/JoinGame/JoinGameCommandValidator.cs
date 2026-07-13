using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.JoinGame
{
    public sealed class JoinGameCommandValidator : AbstractValidator<JoinGameCommand>
    {
        public JoinGameCommandValidator()
        {
            RuleFor(command => command.GameId)
                .NotEmpty();

            RuleFor(command => command.PlayerProfileId)
                .NotEmpty();
        }
    }
}