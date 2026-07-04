using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.UpdateGame
{
    public sealed class UpdateGameCommandHandler : IRequestHandler<UpdateGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateGameCommandHandler(
            IGameRepository gameRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            UpdateGameCommand request,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            game.UpdateDetails(
                request.CourtId,
                request.StartsAt,
                request.MaxPlayers,
                request.Description);

            _gameRepository.Update(game);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}