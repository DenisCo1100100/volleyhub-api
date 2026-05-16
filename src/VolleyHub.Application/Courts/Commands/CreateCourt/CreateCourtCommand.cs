using MediatR;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.CreateCourt
{
    public sealed record CreateCourtCommand(
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        CourtSurfaceType SurfaceType,
        bool IsIndoor,
        string? Description) : IRequest<Guid>;
}
