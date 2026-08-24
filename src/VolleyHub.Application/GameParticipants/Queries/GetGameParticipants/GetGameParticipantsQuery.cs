using MediatR;
using VolleyHub.Application.GameParticipants.Common;

namespace VolleyHub.Application.GameParticipants.Queries.GetGameParticipants
{
    public sealed record GetGameParticipantsQuery(Guid GameId)
        : IRequest<IReadOnlyList<GameParticipantSummaryDto>>;
}