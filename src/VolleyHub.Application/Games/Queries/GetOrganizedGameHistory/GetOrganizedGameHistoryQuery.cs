using MediatR;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Queries.GetOrganizedGameHistory
{
    public sealed record GetOrganizedGameHistoryQuery(
        int Page = 1,
        int PageSize = 20,
        GameHistoryPeriod Period = GameHistoryPeriod.All,
        GameStatus? Status = null,
        DateTimeOffset? StartsAtFrom = null,
        DateTimeOffset? StartsAtTo = null,
        Guid? CourtId = null)
        : IRequest<PagedResult<OrganizedGameHistoryDto>>;
}
