using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.JoinGameWaitlist
{
    public sealed class JoinGameWaitlistCommandValidator : AbstractValidator<JoinGameWaitlistCommand>
    {
        public JoinGameWaitlistCommandValidator()
        {
            RuleFor(command => command.GameId).NotEmpty();
        }
    }
}
