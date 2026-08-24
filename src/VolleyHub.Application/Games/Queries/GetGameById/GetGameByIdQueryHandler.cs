using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Common;
using VolleyHub.Application.GameParticipants.Mappings;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.Games.Mappings;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.Games;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Queries.GetGameById
{
    public sealed class GetGameByIdQueryHandler : IRequestHandler<GetGameByIdQuery, GameDetailsDto>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ICourtRepository _courtRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetGameByIdQueryHandler(
            IGameRepository gameRepository,
            ICourtRepository courtRepository,
            IGameParticipantRepository gameParticipantRepository,
            ICurrentUserService currentUserService,
            IPlayerProfileRepository playerProfileRepository)
        {
            _gameRepository = gameRepository;
            _courtRepository = courtRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _currentUserService = currentUserService;
            _playerProfileRepository = playerProfileRepository;
        }

        public async Task<GameDetailsDto> Handle(
            GetGameByIdQuery request,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetByIdAsync(request.Id, cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            var court = await _courtRepository.GetByIdAsync(game.CourtId, cancellationToken);

            if (court is null || court.IsDeleted)
            {
                throw new NotFoundException(nameof(Court), game.CourtId);
            }

            var organizer = await _playerProfileRepository.GetByIdAsync(game.OrganizerId, cancellationToken);

            if (organizer is null || organizer.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), game.OrganizerId);
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                game.Id,
                cancellationToken);

            var participantSummaries = new List<GameParticipantSummaryDto>(participants.Count);

            foreach (var participant in participants)
            {
                var playerProfile = await _playerProfileRepository.GetByIdAsync(
                    participant.PlayerProfileId,
                    cancellationToken);

                if (playerProfile is null || playerProfile.IsDeleted)
                {
                    throw new NotFoundException(nameof(PlayerProfile), participant.PlayerProfileId);
                }

                participantSummaries.Add(participant.ToSummaryDto(playerProfile));
            }

            var currentPlayerProfileId = await GetCurrentPlayerProfileIdAsync(cancellationToken);

            return game.ToDetailsDto(
                court,
                organizer,
                participantSummaries,
                currentPlayerProfileId);
        }

        private async Task<Guid?> GetCurrentPlayerProfileIdAsync(CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                return null;
            }

            var playerProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            return playerProfile?.IsDeleted is true
                ? null
                : playerProfile?.Id;
        }
    }
}