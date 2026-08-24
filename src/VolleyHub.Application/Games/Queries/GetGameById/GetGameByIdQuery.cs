using MediatR;
using VolleyHub.Application.Games.Common;

namespace VolleyHub.Application.Games.Queries.GetGameById
{
    public sealed record GetGameByIdQuery(Guid Id) : IRequest<GameDetailsDto>;
}