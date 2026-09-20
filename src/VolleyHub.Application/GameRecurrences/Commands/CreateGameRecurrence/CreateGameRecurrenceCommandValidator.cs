using FluentValidation;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameRecurrences.Commands.CreateGameRecurrence
{
    public sealed class CreateGameRecurrenceCommandValidator : AbstractValidator<CreateGameRecurrenceCommand>
    {
        public CreateGameRecurrenceCommandValidator(IDateTimeProvider clock)
        {
            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.SourceGameId).NotEmpty();
            RuleFor(command => command.FirstStartsAt).Must(value => value > clock.UtcNow)
                .WithMessage("The first occurrence must start in the future.");
            RuleFor(command => command.OccurrenceCount).InclusiveBetween(GameRecurrence.MinOccurrences, GameRecurrence.MaxOccurrences);
        }
    }
}
