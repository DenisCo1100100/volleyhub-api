using MediatR;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.UpdateCourt
{
    public sealed record UpdateCourtCommand(
        Guid Id,
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        CourtSurfaceType SurfaceType,
        bool IsIndoor,
        string? Description) : IRequest<Unit>;
}
