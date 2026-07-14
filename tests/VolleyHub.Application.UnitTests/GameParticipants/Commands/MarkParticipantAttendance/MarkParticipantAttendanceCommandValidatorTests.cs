using FluentValidation.TestHelper;
using VolleyHub.Application.GameParticipants.Commands.MarkParticipantAttendance;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.UnitTests.GameParticipants.Commands.MarkParticipantAttendance
{
    public sealed class MarkParticipantAttendanceCommandValidatorTests
    {
        private readonly MarkParticipantAttendanceCommandValidator _validator = new();

        [Theory]
        [InlineData(GameParticipantAttendanceStatus.Present)]
        [InlineData(GameParticipantAttendanceStatus.Absent)]
        public void Validate_ShouldNotHaveValidationErrors_WhenCommandIsValid(
            GameParticipantAttendanceStatus attendanceStatus)
        {
            var command = new MarkParticipantAttendanceCommand(
                ParticipantId: Guid.NewGuid(),
                AttendanceStatus: attendanceStatus);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldHaveValidationError_WhenParticipantIdIsEmpty()
        {
            var command = new MarkParticipantAttendanceCommand(
                ParticipantId: Guid.Empty,
                AttendanceStatus: GameParticipantAttendanceStatus.Present);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.ParticipantId);
        }

        [Theory]
        [InlineData(GameParticipantAttendanceStatus.Unknown)]
        [InlineData(GameParticipantAttendanceStatus.NotMarked)]
        [InlineData((GameParticipantAttendanceStatus)999)]
        public void Validate_ShouldHaveValidationError_WhenAttendanceStatusIsInvalid(
            GameParticipantAttendanceStatus attendanceStatus)
        {
            var command = new MarkParticipantAttendanceCommand(
                ParticipantId: Guid.NewGuid(),
                AttendanceStatus: attendanceStatus);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(command => command.AttendanceStatus);
        }
    }
}