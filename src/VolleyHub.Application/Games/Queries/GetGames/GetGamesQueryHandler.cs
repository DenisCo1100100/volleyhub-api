using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Dtos;
using VolleyHub.Application.Games.Mappings;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, IReadOnlyList<GameDto>>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetGamesQueryHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            ICurrentUserService currentUserService,
            IPlayerProfileRepository playerProfileRepository)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _currentUserService = currentUserService;
            _playerProfileRepository = playerProfileRepository;
        }

        public async Task<IReadOnlyList<GameDto>> Handle(
            GetGamesQuery request,
            CancellationToken cancellationToken)
        {
            var games = await _gameRepository.GetListAsync(cancellationToken);
            var currentPlayerProfileId = await GetCurrentPlayerProfileIdAsync(cancellationToken);

            var result = new List<GameDto>(games.Count);

            foreach (var game in games)
            {
                var participants = await _gameParticipantRepository.GetByGameIdAsync(
                    game.Id,
                    cancellationToken);

                result.Add(game.ToDto(participants, currentPlayerProfileId));
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