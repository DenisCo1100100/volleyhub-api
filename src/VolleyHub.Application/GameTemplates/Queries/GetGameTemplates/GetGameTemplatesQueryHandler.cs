using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Queries.GetGameTemplates
{
    public sealed class GetGameTemplatesQueryHandler : IRequestHandler<GetGameTemplatesQuery, IReadOnlyList<GameTemplateDto>>
    {
        private readonly IGameTemplateRepository _templates;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;

        public GetGameTemplatesQueryHandler(IGameTemplateRepository templates, IPlayerProfileRepository profiles, ICurrentUserService currentUser)
        {
            _templates = templates;
            _profiles = profiles;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<GameTemplateDto>> Handle(GetGameTemplatesQuery request, CancellationToken cancellationToken)
        {
            var organizer = await GameTemplateAccess.GetOrganizerAsync(_currentUser, _profiles, cancellationToken);
            var templates = await _templates.GetByOrganizerIdAsync(organizer.Id, cancellationToken);
            return templates.Select(template => template.ToDto()).ToArray();
        }
    }
}
