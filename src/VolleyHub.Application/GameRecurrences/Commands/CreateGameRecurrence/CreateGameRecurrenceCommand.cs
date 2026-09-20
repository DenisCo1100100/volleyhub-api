using MediatR;

namespace VolleyHub.Application.GameRecurrences.Commands.CreateGameRecurrence
{
    public sealed record CreateGameRecurrenceCommand(Guid Id, Guid SourceGameId, DateTimeOffset FirstStartsAt, int OccurrenceCount) : IRequest<Guid>;
}
