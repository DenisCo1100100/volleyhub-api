using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.Games.Commands.CancelGame;
using VolleyHub.Application.Games.Commands.CompleteGame;
using VolleyHub.Application.Games.Commands.CreateGame;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Application.Games.Queries.GetGameById;
using VolleyHub.Application.Games.Queries.GetGames;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetGames(CancellationToken cancellationToken)
        {
            var games = await _sender.Send(new GetGamesQuery(), cancellationToken);

            return Ok(games);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetGameById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var game = await _sender.Send(new GetGameByIdQuery(id), cancellationToken);

            return Ok(game);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateGame(
            CreateGameRequest request,
            CancellationToken cancellationToken)
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
        public async Task<IActionResult> UpdateGame(
            Guid id,
            UpdateGameRequest request,
            CancellationToken cancellationToken)
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
        public async Task<IActionResult> CancelGame(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new CancelGameCommand(id), cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> CompleteGame(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new CompleteGameCommand(id), cancellationToken);

            return NoContent();
        }
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