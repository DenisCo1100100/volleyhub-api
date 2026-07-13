using MediatR;

namespace VolleyHub.Application.GameParticipants.Commands.ApproveParticipant
{
    public sealed record ApproveParticipantCommand(Guid ParticipantId) : IRequest;
}