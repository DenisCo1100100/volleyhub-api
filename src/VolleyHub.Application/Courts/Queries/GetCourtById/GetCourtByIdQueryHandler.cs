using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Dtos;
using VolleyHub.Application.Courts.Mappings;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Queries.GetCourtById
{
    public sealed class GetCourtByIdQueryHandler
        : IRequestHandler<GetCourtByIdQuery, CourtDto>
    {
        private readonly ICourtRepository _courtRepository;

        public GetCourtByIdQueryHandler(ICourtRepository courtRepository)
        {
            _courtRepository = courtRepository;
        }

        public async Task<CourtDto> Handle(
            GetCourtByIdQuery request,
            CancellationToken cancellationToken)
        {
            var court = await _courtRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (court is null)
            {
                throw new NotFoundException(nameof(Court), request.Id);
            }

            return court.ToDto();
        }
    }
}