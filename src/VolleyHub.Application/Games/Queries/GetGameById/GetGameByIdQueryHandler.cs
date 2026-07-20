using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Dtos;
using VolleyHub.Application.Games.Mappings;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Queries.GetGameById
{
    public sealed class GetGameByIdQueryHandler : IRequestHandler<GetGameByIdQuery, GameDto>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetGameByIdQueryHandler(
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

        public async Task<GameDto> Handle(
            GetGameByIdQuery request,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                game.Id,
                cancellationToken);

            var currentPlayerProfileId = await GetCurrentPlayerProfileIdAsync(cancellationToken);

            return game.ToDto(participants, currentPlayerProfileId);
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