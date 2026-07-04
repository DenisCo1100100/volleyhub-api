using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.CompleteGame
{
    public sealed class CompleteGameCommandHandler : IRequestHandler<CompleteGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CompleteGameCommandHandler(
            IGameRepository gameRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            CompleteGameCommand request,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), request.Id);
            }

            game.Complete();

            _gameRepository.Update(game);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}