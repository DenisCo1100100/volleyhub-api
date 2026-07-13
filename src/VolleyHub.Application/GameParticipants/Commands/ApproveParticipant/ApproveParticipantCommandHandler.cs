using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.ApproveParticipant
{
    public sealed class ApproveParticipantCommandHandler : IRequestHandler<ApproveParticipantCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public ApproveParticipantCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IDateTimeProvider dateTimeProvider,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _dateTimeProvider = dateTimeProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            ApproveParticipantCommand request,
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

            if (game.Status is not GameStatus.Open)
            {
                throw new InvalidOperationException("Only open games can approve participants.");
            }

            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                participant.GameId,
                cancellationToken);

            var approvedParticipantsCount = participants.Count(
                existingParticipant => existingParticipant.JoinStatus is GameParticipantJoinStatus.Approved);

            if (approvedParticipantsCount >= game.MaxPlayers)
            {
                throw new InvalidOperationException("Game is full.");
            }

            participant.Approve(_dateTimeProvider.UtcNow);

            if (approvedParticipantsCount + 1 >= game.MaxPlayers)
            {
                game.MarkAsFull();
                _gameRepository.Update(game);
            }

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}