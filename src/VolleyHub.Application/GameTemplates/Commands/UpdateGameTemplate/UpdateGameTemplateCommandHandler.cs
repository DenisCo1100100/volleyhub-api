using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Commands.UpdateGameTemplate
{
    public sealed class UpdateGameTemplateCommandHandler : IRequestHandler<UpdateGameTemplateCommand>
    {
        private readonly IGameTemplateRepository _templates;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly ICourtRepository _courts;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateGameTemplateCommandHandler(IGameTemplateRepository templates, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, ICourtRepository courts, IUnitOfWork unitOfWork)
        {
            _templates = templates;
            _profiles = profiles;
            _currentUser = currentUser;
            _courts = courts;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateGameTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await GameTemplateAccess.GetOwnedAsync(request.Id, _templates, _currentUser, _profiles, cancellationToken);
            await GameTemplateAccess.EnsureCourtExistsAsync(request.CourtId, _courts, cancellationToken);
            template.Update(request.Name, request.CourtId, request.Duration, request.MaxPlayers,
                request.PricePerPlayer, request.RequiredLevel, request.JoinPolicy, request.Description);
            _templates.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
