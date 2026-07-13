using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Commands.RejectParticipant
{
    public sealed class RejectParticipantCommandHandler : IRequestHandler<RejectParticipantCommand>
    {
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RejectParticipantCommandHandler(
            IGameParticipantRepository gameParticipantRepository,
            IUnitOfWork unitOfWork)
        {
            _gameParticipantRepository = gameParticipantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            RejectParticipantCommand request,
            CancellationToken cancellationToken)
        {
            var participant = await _gameParticipantRepository.GetByIdAsync(
                request.ParticipantId,
                cancellationToken);

            if (participant is null)
            {
                throw new NotFoundException(nameof(GameParticipant), request.ParticipantId);
            }

            participant.Reject();

            _gameParticipantRepository.Update(participant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}