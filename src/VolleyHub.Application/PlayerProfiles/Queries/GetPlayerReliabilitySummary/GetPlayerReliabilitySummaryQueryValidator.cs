using FluentValidation;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary
{
    public sealed class GetPlayerReliabilitySummaryQueryValidator : AbstractValidator<GetPlayerReliabilitySummaryQuery>
    {
        public GetPlayerReliabilitySummaryQueryValidator()
        {
            RuleFor(query => query.PlayerProfileId)
                .NotEmpty();
        }
    }
}