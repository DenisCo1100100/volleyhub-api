using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.RemoveParticipant
{
    public sealed class RemoveParticipantCommandHandler : IRequestHandler<RemoveParticipantCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveParticipantCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            RemoveParticipantCommand request,
            CancellationToken cancellationToken)
        {
            var participant = await _gameParticipantRepository.GetByIdAsync(
                request.ParticipantId,
                cancellationToken);

            if (participant is null)
            {
                throw new NotFoundException(nameof(GameParticipant), request.ParticipantId);
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