using FluentValidation;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.UpdateParticipantOfflinePayment
{
    public sealed class UpdateParticipantOfflinePaymentCommandValidator : AbstractValidator<UpdateParticipantOfflinePaymentCommand>
    {
        public UpdateParticipantOfflinePaymentCommandValidator()
        {
            RuleFor(command => command.ParticipantId).NotEmpty();
            RuleFor(command => command.OfflinePaymentStatus)
                .Must(status => status is not GameParticipantOfflinePaymentStatus.Unknown && Enum.IsDefined(status))
                .WithMessage("Offline payment status is invalid.");
        }
    }
}
