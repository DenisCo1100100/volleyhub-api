using MediatR;

namespace VolleyHub.Application.GameTemplates.Commands.CreateGameFromTemplate
{
    public sealed record CreateGameFromTemplateCommand(Guid Id, DateTimeOffset StartsAt) : IRequest<Guid>;
}
