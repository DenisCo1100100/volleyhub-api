using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Common;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameTemplates.Commands.CreateGameTemplate
{
    public sealed class CreateGameTemplateCommandHandler : IRequestHandler<CreateGameTemplateCommand, Guid>
    {
        private readonly IGameTemplateRepository _templates;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly ICourtRepository _courts;
        private readonly IUnitOfWork _unitOfWork;

        public CreateGameTemplateCommandHandler(IGameTemplateRepository templates, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, ICourtRepository courts, IUnitOfWork unitOfWork)
        {
            _templates = templates;
            _profiles = profiles;
            _currentUser = currentUser;
            _courts = courts;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateGameTemplateCommand request, CancellationToken cancellationToken)
        {
            var organizer = await GameTemplateAccess.GetOrganizerAsync(_currentUser, _profiles, cancellationToken);
            await GameTemplateAccess.EnsureCourtExistsAsync(request.CourtId, _courts, cancellationToken);
            var template = GameTemplate.Create(organizer.Id, request.Name, request.CourtId, request.Duration,
                request.MaxPlayers, request.PricePerPlayer, request.RequiredLevel, request.JoinPolicy, request.Description);
            await _templates.AddAsync(template, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return template.Id;
        }
    }
}
