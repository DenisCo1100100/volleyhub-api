using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.MarkParticipantAttendance
{
    public sealed class MarkParticipantAttendanceCommandHandler : IRequestHandler<MarkParticipantAttendanceCommand>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkParticipantAttendanceCommandHandler(
            IGameRepository gameRepository,
            IGameParticipantRepository gameParticipantRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _gameParticipantRepository = gameParticipantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            MarkParticipantAttendanceCommand request,
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

            if (game.Status is not GameStatus.Completed)
            {
                throw new InvalidOperationException("Attendance can be marked only after the game is completed.");
            }

            participant.MarkAttendance(request.AttendanceStatus);

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}