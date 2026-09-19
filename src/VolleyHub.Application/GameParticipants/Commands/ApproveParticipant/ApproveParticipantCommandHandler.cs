using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Common;

namespace VolleyHub.Application.GameParticipants.Commands.ApproveParticipant
{
    public sealed class ApproveParticipantCommandHandler : IRequestHandler<ApproveParticipantCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public ApproveParticipantCommandHandler(
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
            ApproveParticipantCommand request,
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

            if (game.Status is not GameStatus.Open)
            {
                throw new BusinessRuleException("Only open games can approve participants.");
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                participant.GameId,
                cancellationToken);

            var approvedParticipantsCount = participants.Count(
                existingParticipant => existingParticipant.JoinStatus is GameParticipantJoinStatus.Approved);

            if (participants.Any(existingParticipant => existingParticipant.JoinStatus is GameParticipantJoinStatus.Waitlisted))
            {
                throw new BusinessRuleException("Available places must be assigned from the waitlist first.");
            }

            if (approvedParticipantsCount >= game.MaxPlayers)
            {
                throw new BusinessRuleException("Game is full.");
            }

            participant.Approve(_dateTimeProvider.UtcNow);

            if (approvedParticipantsCount + 1 >= game.MaxPlayers)
            {
                game.MarkAsFull();
                _gameRepository.Update(game);
            }

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
