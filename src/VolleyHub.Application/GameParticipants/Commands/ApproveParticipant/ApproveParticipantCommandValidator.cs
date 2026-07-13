using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Commands.ApproveParticipant
{
    public sealed class ApproveParticipantCommandValidator : AbstractValidator<ApproveParticipantCommand>
    {
        public ApproveParticipantCommandValidator()
        {
            RuleFor(command => command.ParticipantId)
                .NotEmpty();
        }
    }
}