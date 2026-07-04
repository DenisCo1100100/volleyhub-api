using MediatR;

namespace VolleyHub.Application.Games.Commands.CancelGame
{
    public sealed record CancelGameCommand(Guid Id) : IRequest;
}