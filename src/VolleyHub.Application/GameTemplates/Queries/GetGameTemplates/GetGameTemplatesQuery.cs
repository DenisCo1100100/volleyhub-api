using MediatR;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Queries.GetGameTemplates
{
    public sealed record GetGameTemplatesQuery() : IRequest<IReadOnlyList<GameTemplateDto>>;
}
