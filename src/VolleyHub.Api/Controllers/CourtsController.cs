using MediatR;
using Microsoft.AspNetCore.Mvc;
using VolleyHub.Application.Courts.Commands.CreateCourt;
using VolleyHub.Application.Courts.Commands.DeleteCourt;
using VolleyHub.Application.Courts.Commands.UpdateCourt;
using VolleyHub.Application.Courts.Queries.GetCourtById;
using VolleyHub.Application.Courts.Queries.GetCourts;
using Microsoft.AspNetCore.Authorization;

namespace VolleyHub.Api.Controllers
{
    [ApiController]
    [Route("api/courts")]
    public sealed class CourtsController : ControllerBase
    {
        private readonly ISender _sender;

        public CourtsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourts(CancellationToken cancellationToken)
        {
            var courts = await _sender.Send(new GetCourtsQuery(), cancellationToken);

            return Ok(courts);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCourtById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var court = await _sender.Send(new GetCourtByIdQuery(id), cancellationToken);

            return Ok(court);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateCourt(
            CreateCourtCommand command,
            CancellationToken cancellationToken)
        {
            var courtId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetCourtById),
                new { id = courtId },
                courtId);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCourt(
            Guid id,
            UpdateCourtCommand command,
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

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCourt(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteCourtCommand(id), cancellationToken);

            return NoContent();
        }
    }
}