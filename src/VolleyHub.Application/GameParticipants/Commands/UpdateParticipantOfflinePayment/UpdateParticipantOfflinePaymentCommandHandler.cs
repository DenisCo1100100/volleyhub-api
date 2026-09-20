using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Commands.UpdateParticipantOfflinePayment
{
    public sealed class UpdateParticipantOfflinePaymentCommandHandler : IRequestHandler<UpdateParticipantOfflinePaymentCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateParticipantOfflinePaymentCommandHandler(IGameRepository gameRepository, IGameParticipantRepository gameParticipantRepository,
            IPlayerProfileRepository playerProfileRepository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateParticipantOfflinePaymentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedException();
            var organizer = await _playerProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken);
            if (organizer is null || organizer.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId);
            }

            var participant = await _gameParticipantRepository.GetByIdAsync(request.ParticipantId, cancellationToken)
                ?? throw new NotFoundException(nameof(GameParticipant), request.ParticipantId);
            var game = await _gameRepository.GetByIdAsync(participant.GameId, cancellationToken)
                ?? throw new NotFoundException(nameof(Game), participant.GameId);

            if (game.OrganizerId != organizer.Id)
            {
                throw new ForbiddenAccessException();
            }

            participant.UpdateOfflinePaymentStatus(game, request.OfflinePaymentStatus);
            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
