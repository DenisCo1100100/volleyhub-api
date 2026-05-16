using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.Courts.Dtos;
using VolleyHub.Application.Courts.Mappings;

namespace VolleyHub.Application.Courts.Queries.GetCourts
{
    public sealed class GetCourtsQueryHandler : IRequestHandler<GetCourtsQuery, IReadOnlyList<CourtDto>>
    {
        private readonly ICourtRepository _courtRepository;

        public GetCourtsQueryHandler(ICourtRepository courtRepository) 
        {
            _courtRepository = courtRepository;
        }

        public async Task<IReadOnlyList<CourtDto>> Handle(
            GetCourtsQuery request,
            CancellationToken cancellationToken)
        {
            var courts = await _courtRepository.GetListAsync(cancellationToken);

            return courts
                .Select(court => court.ToDto())
                .ToList();
        }
    }
}
