using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;

namespace VolleyHub.Application.Courts.Commands.UpdateCourt
{
    public sealed class UpdateCourtCommandHandler : IRequestHandler<UpdateCourtCommand, Unit>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateCourtCommandHandler(
            ICourtRepository courtRepository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _courtRepository = courtRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Unit> Handle(
            UpdateCourtCommand request,
            CancellationToken cancellationToken)
        {
            var court = await _courtRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if(court == null)
                throw new NotFoundException(nameof(court), request.Id);

            court.Update(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.SurfaceType,
                request.IsIndoor,
                request.Description);

            court.MarkCreated(_dateTimeProvider.UtcNow);

            _courtRepository.Update(court);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
