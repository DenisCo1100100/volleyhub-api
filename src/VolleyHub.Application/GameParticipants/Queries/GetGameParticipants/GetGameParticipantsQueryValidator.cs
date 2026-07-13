using FluentValidation;

namespace VolleyHub.Application.GameParticipants.Queries.GetGameParticipants
{
    public sealed class GetGameParticipantsQueryValidator : AbstractValidator<GetGameParticipantsQuery>
    {
        public GetGameParticipantsQueryValidator()
        {
            RuleFor(query => query.GameId)
                .NotEmpty();
        }
    }
}