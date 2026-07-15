using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Commands.CreateGame
{
    public sealed class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateGameCommandHandler(
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

        public async Task<Guid> Handle(
            CreateGameCommand request,
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

            var game = Game.Create(
                organizerProfile.Id,
                request.CourtId,
                request.StartsAt,
                request.EndsAt,
                request.MaxPlayers,
                request.PricePerPlayer,
                request.RequiredLevel,
                request.JoinPolicy,
                request.Description);

            await _gameRepository.AddAsync(game, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return game.Id;
        }
    }
}