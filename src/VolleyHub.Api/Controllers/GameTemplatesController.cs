using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.GameTemplates.Commands.CreateGameFromTemplate;
using VolleyHub.Application.GameTemplates.Commands.CreateGameTemplate;
using VolleyHub.Application.GameTemplates.Commands.DeleteGameTemplate;
using VolleyHub.Application.GameTemplates.Commands.UpdateGameTemplate;
using VolleyHub.Application.GameTemplates.Common;
using VolleyHub.Application.GameTemplates.Queries.GetGameTemplateById;
using VolleyHub.Application.GameTemplates.Queries.GetGameTemplates;
using VolleyHub.Domain.Games;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/game-templates")]
    public sealed class GameTemplatesController : ControllerBase
    {
        private readonly ISender _sender;

        public GameTemplatesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGameTemplateRequest request, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(new CreateGameTemplateCommand(request.Name, request.CourtId, request.Duration,
                request.MaxPlayers, request.PricePerPlayer, request.RequiredLevel, request.JoinPolicy, request.Description), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<GameTemplateDto>>> GetTemplates(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetGameTemplatesQuery(), cancellationToken));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GameTemplateDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetGameTemplateByIdQuery(id), cancellationToken));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateGameTemplateRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new UpdateGameTemplateCommand(id, request.Name, request.CourtId, request.Duration,
                request.MaxPlayers, request.PricePerPlayer, request.RequiredLevel, request.JoinPolicy, request.Description), cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteGameTemplateCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/games")]
        public async Task<IActionResult> CreateGame(Guid id, CreateGameFromTemplateRequest request, CancellationToken cancellationToken)
        {
            var gameId = await _sender.Send(new CreateGameFromTemplateCommand(id, request.StartsAt), cancellationToken);
            return CreatedAtAction(nameof(GamesController.GetGameById), "Games", new { id = gameId }, gameId);
        }
    }

    public sealed record CreateGameTemplateRequest(string Name, Guid CourtId, TimeSpan? Duration, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description);

    public sealed record UpdateGameTemplateRequest(string Name, Guid CourtId, TimeSpan? Duration, int MaxPlayers,
        decimal PricePerPlayer, GameLevel RequiredLevel, GameJoinPolicy JoinPolicy, string? Description);

    public sealed record CreateGameFromTemplateRequest(DateTimeOffset StartsAt);
}
