using MediatR;
using VolleyHub.Application.Courts.Dtos;

namespace VolleyHub.Application.Courts.Queries.GetCourts
{
    public sealed record GetCourtsQuery : IRequest<IReadOnlyList<CourtDto>>;
}
