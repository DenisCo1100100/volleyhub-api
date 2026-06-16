using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.DeleteCourt
{
    public sealed class DeleteCourtCommandHandler : IRequestHandler<DeleteCourtCommand>
    {
        private readonly ICourtRepository _courtRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCourtCommandHandler(
            ICourtRepository courtRepository,
            IUnitOfWork unitOfWork)
        {
            _courtRepository = courtRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeleteCourtCommand request,
            CancellationToken cancellationToken)
        {
            var court = await _courtRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (court is null)
            {
                throw new NotFoundException(nameof(Court), request.Id);
            }

            court.Delete();

            _courtRepository.Update(court);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}