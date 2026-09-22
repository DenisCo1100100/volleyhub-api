using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameTemplates.Common;

namespace VolleyHub.Application.GameTemplates.Commands.CreateGameFromTemplate
{
    public sealed class CreateGameFromTemplateCommandHandler : IRequestHandler<CreateGameFromTemplateCommand, Guid>
    {
        private readonly IGameTemplateRepository _templates;
        private readonly IPlayerProfileRepository _profiles;
        private readonly ICurrentUserService _currentUser;
        private readonly ICourtRepository _courts;
        private readonly IGameRepository _games;
        private readonly IUnitOfWork _unitOfWork;

        public CreateGameFromTemplateCommandHandler(IGameTemplateRepository templates, IPlayerProfileRepository profiles,
            ICurrentUserService currentUser, ICourtRepository courts, IGameRepository games, IUnitOfWork unitOfWork)
        {
            _templates = templates;
            _profiles = profiles;
            _currentUser = currentUser;
            _courts = courts;
            _games = games;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateGameFromTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await GameTemplateAccess.GetOwnedAsync(request.Id, _templates, _currentUser, _profiles, cancellationToken);
            await GameTemplateAccess.EnsureCourtExistsAsync(template.CourtId, _courts, cancellationToken);
            var game = template.CreateGame(request.StartsAt);
            await _games.AddAsync(game, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return game.Id;
        }
    }
}
