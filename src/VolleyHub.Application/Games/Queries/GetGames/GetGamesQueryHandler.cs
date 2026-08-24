using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Common.Models;
using VolleyHub.Application.Games.Common;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, PagedResult<GameSummaryDto>>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetGamesQueryHandler(IGameRepository gameRepository, ICurrentUserService currentUserService)
        {
            _gameRepository = gameRepository;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<GameSummaryDto>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
        {
            var parameters = new GameSummaryQueryParameters(
                request.Page,
                request.PageSize,
                request.StartsAtFrom,
                request.StartsAtTo,
                request.CourtId,
                request.Status,
                _currentUserService.UserId);

            return await _gameRepository.GetSummariesAsync(parameters, cancellationToken);
        }
    }
}