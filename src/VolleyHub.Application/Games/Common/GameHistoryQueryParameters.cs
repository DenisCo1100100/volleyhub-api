using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Common
{
    public sealed record GameHistoryQueryParameters(
        Guid PlayerProfileId,
        DateTimeOffset Now,
        int Page,
        int PageSize,
        GameHistoryPeriod Period,
        GameStatus? Status,
        DateTimeOffset? StartsAtFrom,
        DateTimeOffset? StartsAtTo,
        Guid? CourtId,
        GameParticipantJoinStatus? JoinStatus = null);
}
