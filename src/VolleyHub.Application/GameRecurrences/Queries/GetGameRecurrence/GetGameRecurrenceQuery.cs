using MediatR;
using VolleyHub.Application.GameRecurrences.Common;

namespace VolleyHub.Application.GameRecurrences.Queries.GetGameRecurrence
{
    public sealed record GetGameRecurrenceQuery(Guid Id) : IRequest<GameRecurrenceDto>;
}
