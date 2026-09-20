using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Queries.GetPlayerGameHistory
{
    public sealed class GetPlayerGameHistoryQueryHandler : IRequestHandler<GetPlayerGameHistoryQuery, PagedResult<PlayerGameHistoryDto>>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GetPlayerGameHistoryQueryHandler(IGameRepository gameRepository, IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider)
        {
            _gameRepository = gameRepository;
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<PagedResult<PlayerGameHistoryDto>> Handle(GetPlayerGameHistoryQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedException();
            var playerProfile = await _playerProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken);
            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId);
            }

            var parameters = new GameHistoryQueryParameters(
                playerProfile.Id,
                _dateTimeProvider.UtcNow,
                request.Page,
                request.PageSize,
                request.Period,
                request.Status,
                request.StartsAtFrom?.ToUniversalTime(),
                request.StartsAtTo?.ToUniversalTime(),
                request.CourtId,
                request.JoinStatus);

            return await _gameRepository.GetPlayerHistoryAsync(parameters, cancellationToken);
        }
    }
}
