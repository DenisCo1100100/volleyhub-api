using MediatR;
using VolleyHub.Application.Games.Common;

namespace VolleyHub.Application.Games.Queries.GetGameOfflinePaymentSummary
{
    public sealed record GetGameOfflinePaymentSummaryQuery(Guid GameId) : IRequest<GameOfflinePaymentSummaryDto>;
}
