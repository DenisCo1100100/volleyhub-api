using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.CreateCourt
{
    public sealed class CreateCourtCommandHandler : IRequestHandler<CreateCourtCommand, Guid>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCourtCommandHandler(
            ICourtRepository courtRepository,
            IUnitOfWork unitOfWork)
        {
            _courtRepository = courtRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            CreateCourtCommand request,
            CancellationToken cancellationToken)
        {
            var court = Court.Create(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.SurfaceType,
                request.IsIndoor,
                request.Description);

            await _courtRepository.AddAsync(court, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return court.Id;
        }
    }
}