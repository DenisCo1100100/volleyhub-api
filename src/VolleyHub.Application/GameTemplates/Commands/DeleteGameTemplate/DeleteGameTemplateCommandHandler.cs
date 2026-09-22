using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Commands.DeleteGameTemplate
{
    public sealed class DeleteGameTemplateCommandHandler : IRequestHandler<DeleteGameTemplateCommand>
    {
        private readonly IGameTemplateRepository _templates;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteGameTemplateCommandHandler(IGameTemplateRepository templates, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, IUnitOfWork unitOfWork)
        {
            _templates = templates;
            _profiles = profiles;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteGameTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await GameTemplateAccess.GetOwnedAsync(request.Id, _templates, _currentUser, _profiles, cancellationToken);
            _templates.Delete(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
