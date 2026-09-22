using MediatR;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Queries.GetGameTemplateById
{
    public sealed record GetGameTemplateByIdQuery(Guid Id) : IRequest<GameTemplateDto>;
}
