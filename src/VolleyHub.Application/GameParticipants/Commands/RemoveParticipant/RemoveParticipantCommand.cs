using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.RemoveParticipant
{
    public sealed record RemoveParticipantCommand(Guid ParticipantId) : IRequest;
}