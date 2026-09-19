using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.GameParticipants.Common;
using VolleyHub.Application.GameParticipants.Mappings;
using VolleyHub.Domain.PlayerProfiles;
using VolleyHub.Domain.Games;

namespace VolleyHub.Application.GameParticipants.Queries.GetGameParticipants
{
    public sealed class GetGameParticipantsQueryHandler
        : IRequestHandler<GetGameParticipantsQuery, IReadOnlyList<GameParticipantSummaryDto>>
    {
        private readonly IGameParticipantRepository _gameParticipantRepository;
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetGameParticipantsQueryHandler(
            IGameParticipantRepository gameParticipantRepository,
            IPlayerProfileRepository playerProfileRepository)
        {
            _gameParticipantRepository = gameParticipantRepository;
            _playerProfileRepository = playerProfileRepository;
        }

        public async Task<IReadOnlyList<GameParticipantSummaryDto>> Handle(
            GetGameParticipantsQuery request,
            CancellationToken cancellationToken)
        {
            var participants = await _gameParticipantRepository.GetByGameIdAsync(
                request.GameId,
                cancellationToken);

            var result = new List<GameParticipantSummaryDto>(participants.Count);

            var waitlistPosition = 0;

            foreach (var participant in participants.OrderBy(participant => participant.JoinedAt).ThenBy(participant => participant.Id))
            {
                var playerProfile = await _playerProfileRepository.GetByIdAsync(
                    participant.PlayerProfileId,
                    cancellationToken);

                if (playerProfile is null || playerProfile.IsDeleted)
                {
                    throw new NotFoundException(
                        nameof(PlayerProfile),
                        participant.PlayerProfileId);
                }

                result.Add(participant.ToSummaryDto(playerProfile,
                    participant.JoinStatus is GameParticipantJoinStatus.Waitlisted ? ++waitlistPosition : null));
            }

            return result;
        }
    }
}
