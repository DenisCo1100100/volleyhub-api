using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Commands.JoinGameWaitlist
{
    public sealed class JoinGameWaitlistCommandHandler : IRequestHandler<JoinGameWaitlistCommand, Guid>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _participantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public JoinGameWaitlistCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository participantRepository,
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _participantRepository = participantRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(JoinGameWaitlistCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException();
            var profile = await _playerProfileRepository.GetByUserIdAsync(userId, cancellationToken);

            if (profile is null || profile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), userId);
            }

            var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken)
                ?? throw new NotFoundException(nameof(Game), request.GameId);

            var existingParticipant = await _participantRepository.GetByGameAndPlayerProfileIdAsync(game.Id, profile.Id, cancellationToken);

            if (existingParticipant is not null)
            {
                throw new BusinessRuleException("Player has already joined this game.");
            }

            var participants = await _participantRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var approvedCount = participants.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);
            var participant = GameParticipant.JoinWaitlist(game, profile.Id, _dateTimeProvider.UtcNow, approvedCount);

            await _participantRepository.AddAsync(participant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return participant.Id;
        }
    }
}
