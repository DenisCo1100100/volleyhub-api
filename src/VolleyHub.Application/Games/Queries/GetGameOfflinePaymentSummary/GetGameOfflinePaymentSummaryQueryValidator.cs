using FluentValidation;

namespace VolleyHub.Application.Games.Queries.GetGameOfflinePaymentSummary
{
    public sealed class GetGameOfflinePaymentSummaryQueryValidator : AbstractValidator<GetGameOfflinePaymentSummaryQuery>
    {
        public GetGameOfflinePaymentSummaryQueryValidator()
        {
            RuleFor(query => query.GameId).NotEmpty();
        }
    }
}
