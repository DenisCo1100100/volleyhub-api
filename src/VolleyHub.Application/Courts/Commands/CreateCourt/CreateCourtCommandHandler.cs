using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.CreateCourt
{
    public sealed class CreateCourtCommandHandler : IRequestHandler<CreateCourtCommand, Guid>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateCourtCommandHandler(
            ICourtRepository courtRepository, 
            IUnitOfWork unitOfWork, 
            IDateTimeProvider dateTimeProvider)
        {
            _courtRepository = courtRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
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

            court.MarkCreated(_dateTimeProvider.UtcNow);

            await _courtRepository.AddAsync(court, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return court.Id;
        }
    }
}
