using MediatR;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed record GetGamesQuery(
        int Page = 1,
        int PageSize = 20,
        DateTimeOffset? StartsAtFrom = null,
        DateTimeOffset? StartsAtTo = null,
        Guid? CourtId = null,
        GameStatus? Status = null)
        : IRequest<PagedResult<GameSummaryDto>>;
}