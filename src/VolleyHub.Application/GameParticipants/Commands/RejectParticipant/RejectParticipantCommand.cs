using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.RejectParticipant
{
    public sealed record RejectParticipantCommand(Guid ParticipantId) : IRequest;
}