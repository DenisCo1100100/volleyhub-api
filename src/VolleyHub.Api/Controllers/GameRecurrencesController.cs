using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.GameRecurrences.Commands.CancelGameRecurrence;
using VolleyHub.Application.GameRecurrences.Commands.CreateGameRecurrence;
using VolleyHub.Application.GameRecurrences.Commands.UpdateFutureGames;
using VolleyHub.Application.GameRecurrences.Common;
using VolleyHub.Application.GameRecurrences.Queries.GetGameRecurrence;
using VolleyHub.Domain.Games;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/game-recurrences")]
    public sealed class GameRecurrencesController : ControllerBase
    {
        private readonly ISender _sender;

        public GameRecurrencesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGameRecurrenceRequest request, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(new CreateGameRecurrenceCommand(request.Id, request.SourceGameId,
                request.FirstStartsAt, request.OccurrenceCount), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GameRecurrenceDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetGameRecurrenceQuery(id), cancellationToken));
        }

        [HttpPut("{id:guid}/future")]
        public async Task<IActionResult> UpdateFuture(Guid id, UpdateFutureGamesRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new UpdateFutureGamesCommand(id, request.FromOccurrenceNumber, request.CourtId,
                request.MaxPlayers, request.PricePerPlayer, request.RequiredLevel, request.JoinPolicy, request.Description), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new CancelGameRecurrenceCommand(id), cancellationToken);
            return NoContent();
        }
    }

    public sealed record CreateGameRecurrenceRequest(Guid Id, Guid SourceGameId, DateTimeOffset FirstStartsAt, int OccurrenceCount);

    public sealed record UpdateFutureGamesRequest(int FromOccurrenceNumber, Guid CourtId, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description);
}
