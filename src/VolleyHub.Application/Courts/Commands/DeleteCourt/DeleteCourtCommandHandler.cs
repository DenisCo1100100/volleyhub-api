using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.DeleteCourt
{
    public sealed class DeleteCourtCommandHandler : IRequestHandler<DeleteCourtCommand, Unit>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DeleteCourtCommandHandler(
            ICourtRepository courtRepository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _courtRepository = courtRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Unit> Handle(
            DeleteCourtCommand request,
            CancellationToken cancellationToken)
        {
            var court = await _courtRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (court is null)
                throw new NotFoundException(nameof(Court), request.Id);

            court.Delete();
            court.MarkUpdated(_dateTimeProvider.UtcNow);

            _courtRepository.Update(court);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
