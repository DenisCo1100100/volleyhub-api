using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.RejectParticipant
{
    public sealed class RejectParticipantCommandValidator : AbstractValidator<RejectParticipantCommand>
    {
        public RejectParticipantCommandValidator()
        {
            RuleFor(command => command.ParticipantId)
                .NotEmpty();
        }
    }
}