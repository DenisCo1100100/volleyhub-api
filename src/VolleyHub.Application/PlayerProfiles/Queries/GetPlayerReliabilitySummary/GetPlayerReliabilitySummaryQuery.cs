using MediatR;
using VolleyHub.Application.PlayerProfiles.Dtos;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary
{
    public sealed record GetPlayerReliabilitySummaryQuery(
        Guid PlayerProfileId) : IRequest<PlayerReliabilitySummaryDto>;
}