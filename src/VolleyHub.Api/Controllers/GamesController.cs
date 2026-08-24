using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Commands.CancelGame;
using VolleyHub.Application.Games.Commands.CompleteGame;
using VolleyHub.Application.Games.Commands.CreateGame;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.Games.Queries.GetGameById;
using VolleyHub.Application.Games.Queries.GetGames;
using VolleyHub.Domain.Games;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Route("api/games")]
    public sealed class GamesController : ControllerBase
    {
        private readonly ISender _sender;

        public GamesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<GameSummaryDto>>> GetGames([FromQuery] GetGamesRequest request, CancellationToken cancellationToken)
        {
            var games = await _sender.Send(
                new GetGamesQuery(
                    request.Page,
                    request.PageSize,
                    request.StartsAtFrom,
                    request.StartsAtTo,
                    request.CourtId,
                    request.Status),
                cancellationToken);

            return Ok(games);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GameDetailsDto>> GetGameById(Guid id, CancellationToken cancellationToken)
        {
            var game = await _sender.Send(new GetGameByIdQuery(id), cancellationToken);

            return Ok(game);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateGame(CreateGameRequest request, CancellationToken cancellationToken)
        {
            var gameId = await _sender.Send(
                new CreateGameCommand(
                    request.CourtId,
                    request.StartsAt,
                    request.EndsAt,
                    request.MaxPlayers,
                    request.PricePerPlayer,
                    request.RequiredLevel,
                    request.JoinPolicy,
                    request.Description),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetGameById),
                new { id = gameId },
                gameId);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateGame(Guid id, UpdateGameRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(
                new UpdateGameCommand(
                    id,
                    request.CourtId,
                    request.StartsAt,
                    request.EndsAt,
                    request.MaxPlayers,
                    request.PricePerPlayer,
                    request.RequiredLevel,
                    request.JoinPolicy,
                    request.Description),
                cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelGame(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new CancelGameCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> CompleteGame(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new CompleteGameCommand(id), cancellationToken);

            return NoContent();
        }
    }

    public sealed class GetGamesRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public DateTimeOffset? StartsAtFrom { get; init; }
        public DateTimeOffset? StartsAtTo { get; init; }
        public Guid? CourtId { get; init; }
        public GameStatus? Status { get; init; }
    }

    public sealed record CreateGameRequest(
        Guid CourtId,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int MaxPlayers,
        decimal PricePerPlayer,
        GameLevel RequiredLevel,
        GameJoinPolicy JoinPolicy,
        string? Description);

    public sealed record UpdateGameRequest(
        Guid CourtId,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int MaxPlayers,
        decimal PricePerPlayer,
        GameLevel RequiredLevel,
        GameJoinPolicy JoinPolicy,
        string? Description);
}