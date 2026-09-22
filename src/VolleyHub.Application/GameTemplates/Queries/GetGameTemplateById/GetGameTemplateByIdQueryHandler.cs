using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Queries.GetGameTemplateById
{
    public sealed class GetGameTemplateByIdQueryHandler : IRequestHandler<GetGameTemplateByIdQuery, GameTemplateDto>
    {
        private readonly IGameTemplateRepository _templates;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;

        public GetGameTemplateByIdQueryHandler(IGameTemplateRepository templates, IPlayerProfileRepository profiles, ICurrentUserService currentUser)
        {
            _templates = templates;
            _profiles = profiles;
            _currentUser = currentUser;
        }

        public async Task<GameTemplateDto> Handle(GetGameTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            var template = await GameTemplateAccess.GetOwnedAsync(request.Id, _templates, _currentUser, _profiles, cancellationToken);
            return template.ToDto();
        }
    }
}
