using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Commands.RejectParticipant
{
    public sealed class RejectParticipantCommandHandler : IRequestHandler<RejectParticipantCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public RejectParticipantCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            RejectParticipantCommand request,
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

            var participant = await _gameParticipantRepository.GetByIdAsync(
                request.ParticipantId,
                cancellationToken);

            if (participant is null)
            {
                throw new NotFoundException(nameof(GameParticipant), request.ParticipantId);
            }

            var game = await _gameRepository.GetByIdAsync(
                participant.GameId,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), participant.GameId);
            }

            if (game.OrganizerId != organizerProfile.Id)
            {
                throw new ForbiddenAccessException();
            }

            participant.Reject();

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}