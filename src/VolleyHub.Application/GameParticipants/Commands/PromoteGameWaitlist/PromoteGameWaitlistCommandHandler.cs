using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Common;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.GameParticipants.Commands.PromoteGameWaitlist
{
    public sealed class PromoteGameWaitlistCommandHandler : IRequestHandler<PromoteGameWaitlistCommand, Guid>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _participantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public PromoteGameWaitlistCommandHandler(
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

        public async Task<Guid> Handle(PromoteGameWaitlistCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException();
            var organizer = await _playerProfileRepository.GetByUserIdAsync(userId, cancellationToken);

            if (organizer is null || organizer.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), userId);
            }

            var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken)
                ?? throw new NotFoundException(nameof(Game), request.GameId);

            if (game.OrganizerId != organizer.Id)
            {
                throw new ForbiddenAccessException();
            }

            var participants = await _participantRepository.GetByGameIdAsync(game.Id, cancellationToken);
            var approvedCount = participants.Count(participant => participant.JoinStatus is GameParticipantJoinStatus.Approved);
            var now = _dateTimeProvider.UtcNow;
            game.EnsureCanPromoteFromWaitlist(approvedCount, now);

            foreach (var candidate in participants.Where(participant => participant.JoinStatus is GameParticipantJoinStatus.Waitlisted)
                .OrderBy(participant => participant.JoinedAt).ThenBy(participant => participant.Id))
            {
                var profile = await _playerProfileRepository.GetByIdAsync(candidate.PlayerProfileId, cancellationToken);

                if (profile is null || profile.IsDeleted)
                {
                    continue;
                }

                // Load the tracked record so a concurrent withdrawal is detected on save.
                var participant = await _participantRepository.GetByIdAsync(candidate.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(GameParticipant), candidate.Id);

                participant.PromoteFromWaitlist(game, approvedCount, now);
                _participantRepository.Update(participant);
                _gameRepository.Update(game);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return participant.Id;
            }

            throw new BusinessRuleException("There are no eligible players on the waitlist.");
        }
    }
}
