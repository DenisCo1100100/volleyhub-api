using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.PromoteGameWaitlist
{
    public sealed class PromoteGameWaitlistCommandValidator : AbstractValidator<PromoteGameWaitlistCommand>
    {
        public PromoteGameWaitlistCommandValidator()
        {
            RuleFor(command => command.GameId).NotEmpty();
        }
    }
}
