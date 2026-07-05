using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.Games.Commands.CancelGame;
using VolleyHub.Application.Games.Commands.CompleteGame;
using VolleyHub.Application.Games.Commands.CreateGame;
using VolleyHub.Application.Games.Commands.UpdateGame;
using VolleyHub.Application.Games.Queries.GetGameById;
using VolleyHub.Application.Games.Queries.GetGames;

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

        [HttpPost]
        public async Task<IActionResult> CreateGame(
            CreateGameCommand command,
            CancellationToken cancellationToken)
        {
            var gameId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetGameById),
                new { id = gameId },
                gameId);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateGame(
            Guid id,
            UpdateGameCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad request",
                    detail: "Route id and body id must be the same.");
            }

            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelGame(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new CancelGameCommand(id), cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> CompleteGame(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new CompleteGameCommand(id), cancellationToken);

            return NoContent();
        }
    }
}