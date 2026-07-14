using MediatR;
using VolleyHub.Application.PlayerProfiles.Dtos;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById
{
    public sealed record GetPlayerProfileByIdQuery(Guid Id) : IRequest<PlayerProfileDto>;
}