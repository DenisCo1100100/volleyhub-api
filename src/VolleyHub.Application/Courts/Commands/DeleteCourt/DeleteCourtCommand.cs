using MediatR;

namespace VolleyHub.Application.Courts.Commands.DeleteCourt
{
    public sealed record DeleteCourtCommand(Guid Id) : IRequest;
}