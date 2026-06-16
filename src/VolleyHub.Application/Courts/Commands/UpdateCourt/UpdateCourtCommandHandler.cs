using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.UpdateCourt
{
    public sealed class UpdateCourtCommandHandler : IRequestHandler<UpdateCourtCommand>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCourtCommandHandler(
            ICourtRepository courtRepository,
            IUnitOfWork unitOfWork)
        {
            _courtRepository = courtRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            UpdateCourtCommand request,
            CancellationToken cancellationToken)
        {
            var court = await _courtRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (court is null)
            {
                throw new NotFoundException(nameof(Court), request.Id);
            }

            court.Update(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.SurfaceType,
                request.IsIndoor,
                request.Description);

            _courtRepository.Update(court);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}