using MediatR;
using VolleyHub.Application.PlayerProfiles.Dtos;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetCurrentPlayerProfile
{
    public sealed record GetCurrentPlayerProfileQuery : IRequest<PlayerProfileDto>;
}