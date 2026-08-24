using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Games.Common
{
    public sealed record GameCourtSummaryDto(
        Guid Id,
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        CourtSurfaceType SurfaceType,
        bool IsIndoor);
}