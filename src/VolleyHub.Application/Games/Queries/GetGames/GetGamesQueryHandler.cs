using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Games.Dtos;
using VolleyHub.Application.Games.Mappings;

namespace VolleyHub.Application.Games.Queries.GetGames
{
    public sealed class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, IReadOnlyList<GameDto>>
    {
        private readonly IGameRepository _gameRepository;

        public GetGamesQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IReadOnlyList<GameDto>> Handle(
            GetGamesQuery request,
            CancellationToken cancellationToken)
        {
            var games = await _gameRepository.GetListAsync(cancellationToken);

            return games
                .Select(game => game.ToDto())
                .ToList();
        }
    }
}