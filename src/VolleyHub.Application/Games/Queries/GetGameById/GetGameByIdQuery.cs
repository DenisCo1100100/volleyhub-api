using MediatR;
using VolleyHub.Application.Games.Dtos;

namespace VolleyHub.Application.Games.Queries.GetGameById
{
    public sealed record GetGameByIdQuery(Guid Id) : IRequest<GameDto>;
}