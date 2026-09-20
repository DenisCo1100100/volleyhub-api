using MediatR;

namespace VolleyHub.Application.GameRecurrences.Commands.CancelGameRecurrence
{
    public sealed record CancelGameRecurrenceCommand(Guid Id) : IRequest;
}
