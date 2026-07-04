using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.CancelGame
{
    public sealed class CancelGameCommandHandler : IRequestHandler<CancelGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CancelGameCommandHandler(
            IGameRepository gameRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            CancelGameCommand request,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            game.Cancel();

            _gameRepository.Update(game);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}