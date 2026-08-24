using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Common
{
    public sealed record GameSummaryQueryParameters(
        int Page,
        int PageSize,
        DateTimeOffset? StartsAtFrom,
        DateTimeOffset? StartsAtTo,
        Guid? CourtId,
        GameStatus? Status,
        Guid? CurrentUserId);
}