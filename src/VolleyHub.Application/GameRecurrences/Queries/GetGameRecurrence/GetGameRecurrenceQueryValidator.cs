using FluentValidation;

namespace VolleyHub.Application.GameRecurrences.Queries.GetGameRecurrence
{
    public sealed class GetGameRecurrenceQueryValidator : AbstractValidator<GetGameRecurrenceQuery>
    {
        public GetGameRecurrenceQueryValidator()
        {
            RuleFor(request => request.Id).NotEmpty();
        }
    }
}
