using FluentValidation;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.MarkParticipantAttendance
{
    public sealed class MarkParticipantAttendanceCommandValidator : AbstractValidator<MarkParticipantAttendanceCommand>
    {
        public MarkParticipantAttendanceCommandValidator()
        {
            RuleFor(command => command.ParticipantId)
                .NotEmpty();

            RuleFor(command => command.AttendanceStatus)
                .Must(attendanceStatus =>
                    attendanceStatus is not GameParticipantAttendanceStatus.Unknown
                    && attendanceStatus is not GameParticipantAttendanceStatus.NotMarked
                    && Enum.IsDefined(attendanceStatus))
                .WithMessage("Attendance status is invalid.");
        }
    }
}