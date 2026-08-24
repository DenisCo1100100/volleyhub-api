using MediatR;
using VolleyHub.Application.Games.Common;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed record GetGamesQuery : IRequest<IReadOnlyList<GameSummaryDto>>;
}