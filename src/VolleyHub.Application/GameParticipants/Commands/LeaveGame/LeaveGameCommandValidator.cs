using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.LeaveGame
{
    public sealed class LeaveGameCommandValidator : AbstractValidator<LeaveGameCommand>
    {
        public LeaveGameCommandValidator()
        {
            RuleFor(command => command.GameId)
                .NotEmpty();
        }
    }
}