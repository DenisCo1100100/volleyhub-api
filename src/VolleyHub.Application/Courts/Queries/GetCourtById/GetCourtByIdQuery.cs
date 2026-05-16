using MediatR;
using VolleyHub.Application.Courts.Dtos;

namespace VolleyHub.Application.Courts.Queries.GetCourtById
{
    public sealed record GetCourtByIdQuery(Guid Id) : IRequest<CourtDto>;
}
