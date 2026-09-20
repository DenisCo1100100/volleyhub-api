using FluentValidation;

namespace VolleyHub.Application.Games.Queries.GetOrganizedGameHistory
{
    public sealed class GetOrganizedGameHistoryQueryValidator : AbstractValidator<GetOrganizedGameHistoryQuery>
    {
        public GetOrganizedGameHistoryQueryValidator()
        {
            RuleFor(query => query.Page).GreaterThan(0);
            RuleFor(query => query.PageSize).InclusiveBetween(1, 100);

            RuleFor(query => query)
                .Must(query => ((long)query.Page - 1) * query.PageSize <= int.MaxValue)
                .WithMessage("The requested page offset is too large.");

            RuleFor(query => query.Period).IsInEnum();

            RuleFor(query => query.Status)
                .Must(status => status is null || Enum.IsDefined(status.Value))
                .WithMessage("Game status is invalid.");

            RuleFor(query => query.CourtId)
                .Must(courtId => courtId is null || courtId != Guid.Empty)
                .WithMessage("Court id must not be empty.");

            RuleFor(query => query)
                .Must(query => query.StartsAtFrom is null || query.StartsAtTo is null || query.StartsAtFrom <= query.StartsAtTo)
                .WithMessage("StartsAtFrom must be earlier than or equal to StartsAtTo.");
        }
    }
}
