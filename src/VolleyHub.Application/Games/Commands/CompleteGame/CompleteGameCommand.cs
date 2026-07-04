using MediatR;

namespace VolleyHub.Application.Games.Commands.CompleteGame
{
    public sealed record CompleteGameCommand(Guid Id) : IRequest;
}