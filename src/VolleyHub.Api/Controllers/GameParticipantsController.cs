using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.GameParticipants.Commands.ApproveParticipant;
using VolleyHub.Application.GameParticipants.Commands.JoinGame;
using VolleyHub.Application.GameParticipants.Commands.LeaveGame;
using VolleyHub.Application.GameParticipants.Commands.RejectParticipant;
using VolleyHub.Application.GameParticipants.Commands.RemoveParticipant;
using VolleyHub.Application.GameParticipants.Queries.GetGameParticipants;
using VolleyHub.Application.GameParticipants.Commands.MarkParticipantAttendance;
using VolleyHub.Domain.Games;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public sealed class GameParticipantsController : ControllerBase
    {
        private readonly ISender _sender;

        public GameParticipantsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("games/{gameId:guid}/participants")]
        public async Task<IActionResult> GetGameParticipants(
            Guid gameId,
            CancellationToken cancellationToken)
        {
            var participants = await _sender.Send(
                new GetGameParticipantsQuery(gameId),
                cancellationToken);

            return Ok(participants);
        }

        [HttpPost("games/{gameId:guid}/participants")]
        public async Task<IActionResult> JoinGame(
            Guid gameId,
            JoinGameRequest request,
            CancellationToken cancellationToken)
        {
            var participantId = await _sender.Send(
                new JoinGameCommand(
                    gameId,
                    request.PlayerProfileId),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetGameParticipants),
                new { gameId },
                participantId);
        }

        [HttpPost("game-participants/{participantId:guid}/approve")]
        public async Task<IActionResult> ApproveParticipant(
            Guid participantId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new ApproveParticipantCommand(participantId),
                cancellationToken);

            return NoContent();
        }

        [HttpPost("game-participants/{participantId:guid}/reject")]
        public async Task<IActionResult> RejectParticipant(
            Guid participantId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new RejectParticipantCommand(participantId),
                cancellationToken);

            return NoContent();
        }

        [HttpPost("game-participants/{participantId:guid}/attendance")]
        public async Task<IActionResult> MarkParticipantAttendance(
            Guid participantId,
            MarkParticipantAttendanceRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new MarkParticipantAttendanceCommand(
                    participantId,
                    request.AttendanceStatus),
                cancellationToken);

            return NoContent();
        }

        [HttpPost("games/{gameId:guid}/participants/leave")]
        public async Task<IActionResult> LeaveGame(
            Guid gameId,
            LeaveGameRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new LeaveGameCommand(
                    gameId,
                    request.PlayerProfileId),
                cancellationToken);

            return NoContent();
        }

        [HttpDelete("game-participants/{participantId:guid}")]
        public async Task<IActionResult> RemoveParticipant(
            Guid participantId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new RemoveParticipantCommand(participantId),
                cancellationToken);

            return NoContent();
        }

        public sealed record JoinGameRequest(Guid PlayerProfileId);

        public sealed record LeaveGameRequest(Guid PlayerProfileId);

        public sealed record MarkParticipantAttendanceRequest(GameParticipantAttendanceStatus AttendanceStatus);
    }
}