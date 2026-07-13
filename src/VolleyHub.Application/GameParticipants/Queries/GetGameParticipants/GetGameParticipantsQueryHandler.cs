using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Dtos;
using VolleyHub.Application.GameParticipants.Mappings;

namespace VolleyHub.Application.GameParticipants.Queries.GetGameParticipants
{
    public sealed class GetGameParticipantsQueryHandler
        : IRequestHandler<GetGameParticipantsQuery, IReadOnlyList<GameParticipantDto>>
    {
        private readonly IGameParticipantRepository _gameParticipantRepository;

        public GetGameParticipantsQueryHandler(IGameParticipantRepository gameParticipantRepository)
        {
            _gameParticipantRepository = gameParticipantRepository;
        }

        public async Task<IReadOnlyList<GameParticipantDto>> Handle(
            GetGameParticipantsQuery request,
            CancellationToken cancellationToken)
        {
            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                request.GameId,
                cancellationToken);

            return participants
                .Select(participant => participant.ToDto())
                .ToList();
        }
    }
}