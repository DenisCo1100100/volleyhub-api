 using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.PlayerProfiles.Commands.CreatePlayerProfile;
using VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile;
using VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfiles;
using VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary;
using VolleyHub.Application.PlayerProfiles.Queries.GetCurrentPlayerProfile;
using VolleyHub.Application.PlayerProfiles.Commands.UpdateCurrentPlayerProfile;
using Microsoft.AspNetCore.Authorization;
using VolleyHub.Domain.PlayerProfiles;

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

        [HttpGet("{id:guid}/reliability")]
        public async Task<IActionResult> GetPlayerReliabilitySummary(
            Guid id,
            CancellationToken cancellationToken)
        {
            var reliabilitySummary = await _sender.Send(
                new GetPlayerReliabilitySummaryQuery(id),
                cancellationToken);

            return Ok(reliabilitySummary);
        }

        [Authorize]
        [HttpGet("me")]
            public async Task<IActionResult> GetCurrentPlayerProfile(
            CancellationToken cancellationToken)
        {
            var playerProfile = await _sender.Send(
                new GetCurrentPlayerProfileQuery(),
                cancellationToken);

            return Ok(playerProfile);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentPlayerProfile(
            UpdatePlayerProfileRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new UpdateCurrentPlayerProfileCommand(
                    request.DisplayName,
                    request.SkillLevel,
                    request.City,
                    request.Bio),
                cancellationToken);

            return NoContent();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePlayerProfile(
            CreatePlayerProfileRequest request,
            CancellationToken cancellationToken)
        {
            var playerProfileId = await _sender.Send(
                new CreatePlayerProfileCommand(
                    request.DisplayName,
                    request.SkillLevel,
                    request.City,
                    request.Bio),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetPlayerProfileById),
                new { id = playerProfileId },
                playerProfileId);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdatePlayerProfile(
            Guid id,
            UpdatePlayerProfileRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new UpdatePlayerProfileCommand(
                    id,
                    request.DisplayName,
                    request.SkillLevel,
                    request.City,
                    request.Bio),
                cancellationToken);

            return NoContent();
        }

        [Authorize]
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

    public sealed record CreatePlayerProfileRequest(
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        string? City,
        string? Bio);

    public sealed record UpdatePlayerProfileRequest(
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        string? City,
        string? Bio);
}