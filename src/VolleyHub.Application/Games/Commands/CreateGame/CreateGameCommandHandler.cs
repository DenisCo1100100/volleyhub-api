using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.Games.Commands.CreateGame
{
    public sealed class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, Guid>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateGameCommandHandler(
            IGameRepository gameRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            CreateGameCommand request,
            CancellationToken cancellationToken)
        {
            var game = Game.Create(
                request.OrganizerId,
                request.CourtId,
                request.StartsAt,
                request.EndsAt,
                request.MaxPlayers,
                request.PricePerPlayer,
                request.RequiredLevel,
                request.JoinPolicy,
                request.Description);

            await _gameRepository.AddAsync(game, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return game.Id;
        }
    }
}