using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Dtos;
using VolleyHub.Application.Courts.Mappings;

namespace VolleyHub.Application.Courts.Queries.GetCourtById
{
    public sealed class GetCourtsByIdQueryHandler
    {
        private readonly ICourtRepository _courtRepository;

        public GetCourtsByIdQueryHandler(ICourtRepository courtRepository)
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

            if(court is null)
                throw new NotFoundException(nameof(court), request.Id);

            return court.ToDto();
        }
    }
}
