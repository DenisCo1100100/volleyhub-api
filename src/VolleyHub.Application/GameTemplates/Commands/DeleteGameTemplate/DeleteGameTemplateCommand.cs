using MediatR;

namespace VolleyHub.Application.GameTemplates.Commands.DeleteGameTemplate
{
    public sealed record DeleteGameTemplateCommand(Guid Id) : IRequest;
}
