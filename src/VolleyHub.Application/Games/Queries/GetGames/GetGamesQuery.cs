using MediatR;
using VolleyHub.Application.Games.Dtos;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed record GetGamesQuery : IRequest<IReadOnlyList<GameDto>>;
}