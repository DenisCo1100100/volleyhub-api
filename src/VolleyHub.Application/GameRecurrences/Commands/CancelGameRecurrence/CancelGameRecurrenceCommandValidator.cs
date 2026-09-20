using FluentValidation;

namespace VolleyHub.Application.GameRecurrences.Commands.CancelGameRecurrence
{
    public sealed class CancelGameRecurrenceCommandValidator : AbstractValidator<CancelGameRecurrenceCommand>
    {
        public CancelGameRecurrenceCommandValidator()
        {
            RuleFor(request => request.Id).NotEmpty();
        }
    }
}
