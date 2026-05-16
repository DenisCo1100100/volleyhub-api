using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Dtos
{
    public sealed record CourtDto(
        Guid Id,
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        CourtSurfaceType SurfaceType,
        bool IsIndoor,
        string? Description,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdateAt);
}
