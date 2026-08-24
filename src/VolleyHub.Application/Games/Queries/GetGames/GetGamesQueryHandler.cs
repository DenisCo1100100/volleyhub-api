using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Common;
using VolleyHub.Application.Games.Mappings;
using VolleyHub.Domain.Courts;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, IReadOnlyList<GameSummaryDto>>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ICourtRepository _courtRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetGamesQueryHandler(
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

        public async Task<IReadOnlyList<GameSummaryDto>> Handle(
            GetGamesQuery request,
            CancellationToken cancellationToken)
        {
            var games = await _gameRepository.GetListAsync(cancellationToken);
            var currentPlayerProfileId = await GetCurrentPlayerProfileIdAsync(cancellationToken);

            var result = new List<GameSummaryDto>(games.Count);

            foreach (var game in games)
            {
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

                var participants = await _gameParticipantRepository.GetByGameIdAsync(game.Id, cancellationToken);

                result.Add(game.ToSummaryDto(
                    court,
                    organizer,
                    participants,
                    currentPlayerProfileId));
            }

            return result;
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