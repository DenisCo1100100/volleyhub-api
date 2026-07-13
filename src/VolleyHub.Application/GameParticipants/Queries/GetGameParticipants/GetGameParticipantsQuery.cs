using MediatR;
using VolleyHub.Application.GameParticipants.Dtos;

namespace VolleyHub.Application.GameParticipants.Queries.GetGameParticipants
{
    public sealed record GetGameParticipantsQuery(Guid GameId) : IRequest<IReadOnlyList<GameParticipantDto>>;
}