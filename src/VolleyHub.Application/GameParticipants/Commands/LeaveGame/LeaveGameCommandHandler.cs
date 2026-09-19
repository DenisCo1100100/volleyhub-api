using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Commands.LeaveGame
{
    public sealed class LeaveGameCommandHandler : IRequestHandler<LeaveGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public LeaveGameCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            LeaveGameCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var playerProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId.Value);
            }

            var participant = await _gameParticipantRepository.GetByGameAndPlayerProfileIdAsync(
                request.GameId,
                playerProfile.Id,
                cancellationToken);

            if (participant is null)
            {
                throw new NotFoundException(nameof(GameParticipant), playerProfile.Id);
            }

            var game = await _gameRepository.GetByIdAsync(
                participant.GameId,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), participant.GameId);
            }

            var releasedConfirmedPlace =
                participant.JoinStatus is GameParticipantJoinStatus.Approved;

            switch (participant.JoinStatus)
            {
                case GameParticipantJoinStatus.Waitlisted:
                    participant.WithdrawFromWaitlist();
                    break;

                case GameParticipantJoinStatus.PendingApproval:
                    participant.WithdrawRequest();
                    break;

                case GameParticipantJoinStatus.Approved:
                    participant.CancelParticipation(
                        _dateTimeProvider.UtcNow,
                        game.StartsAt);
                    break;

                default:
                    throw new BusinessRuleException(
                        "Only pending or approved participants can leave a game.");
            }

            if (releasedConfirmedPlace && game.Status is GameStatus.Full)
            {
                game.Reopen();
                _gameRepository.Update(game);
            }

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
