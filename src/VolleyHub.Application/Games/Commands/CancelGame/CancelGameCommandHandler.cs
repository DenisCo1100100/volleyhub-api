using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Commands.CancelGame
{
    public sealed class CancelGameCommandHandler : IRequestHandler<CancelGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public CancelGameCommandHandler(
            IGameRepository gameRepository,
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            CancelGameCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var organizerProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (organizerProfile is null || organizerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId.Value);
            }

            var game = await _gameRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            if (game.OrganizerId != organizerProfile.Id)
            {
                throw new ForbiddenAccessException();
            }

            game.Cancel();

            _gameRepository.Update(game);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}