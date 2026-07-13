using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.LeaveGame
{
    public sealed class LeaveGameCommandHandler : IRequestHandler<LeaveGameCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LeaveGameCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            LeaveGameCommand request,
            CancellationToken cancellationToken)
        {
            var participant = await _gameParticipantRepository.GetByGameAndPlayerProfileIdAsync(
                request.GameId,
                request.PlayerProfileId,
                cancellationToken);

            if (participant is null)
            {
                throw new NotFoundException(nameof(GameParticipant), request.PlayerProfileId);
            }

            var game = await _gameRepository.GetByIdAsync(
                participant.GameId,
                cancellationToken);

            if (game is null)
            {
                throw new NotFoundException(nameof(Game), participant.GameId);
            }

            var wasApproved = participant.JoinStatus is GameParticipantJoinStatus.Approved;

            participant.Cancel();

            if (wasApproved && game.Status is GameStatus.Full)
            {
                game.Reopen();
                _gameRepository.Update(game);
            }

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}