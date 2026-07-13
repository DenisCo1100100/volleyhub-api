using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.RemoveParticipant
{
    public sealed class RemoveParticipantCommandValidator : AbstractValidator<RemoveParticipantCommand>
    {
        public RemoveParticipantCommandValidator()
        {
            RuleFor(command => command.ParticipantId)
                .NotEmpty();
        }
    }
}