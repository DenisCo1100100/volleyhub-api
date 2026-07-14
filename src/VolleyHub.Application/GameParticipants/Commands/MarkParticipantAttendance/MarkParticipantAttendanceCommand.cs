using MediatR;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.MarkParticipantAttendance
{
    public sealed record MarkParticipantAttendanceCommand(
        Guid ParticipantId,
        GameParticipantAttendanceStatus AttendanceStatus) : IRequest;
}