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

        public GetGameByIdQueryHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
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

            return game.ToDto();
        }
    }
}