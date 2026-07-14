using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.PlayerProfiles.Commands.CreatePlayerProfile;
using VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile;
using VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfiles;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Route("api/player-profiles")]
    public sealed class PlayerProfilesController : ControllerBase
    {
        private readonly ISender _sender;

        public PlayerProfilesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayerProfiles(CancellationToken cancellationToken)
        {
            var playerProfiles = await _sender.Send(
                new GetPlayerProfilesQuery(),
                cancellationToken);

            return Ok(playerProfiles);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPlayerProfileById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var playerProfile = await _sender.Send(
                new GetPlayerProfileByIdQuery(id),
                cancellationToken);

            return Ok(playerProfile);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlayerProfile(
            CreatePlayerProfileCommand command,
            CancellationToken cancellationToken)
        {
            var playerProfileId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetPlayerProfileById),
                new { id = playerProfileId },
                playerProfileId);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdatePlayerProfile(
            Guid id,
            UpdatePlayerProfileCommand command,
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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeletePlayerProfile(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new DeletePlayerProfileCommand(id),
                cancellationToken);

            return NoContent();
        }
    }
}