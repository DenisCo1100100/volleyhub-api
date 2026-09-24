using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerReliabilitySummary
{
    public sealed class GetPlayerReliabilitySummaryQueryHandler
        : IRequestHandler<GetPlayerReliabilitySummaryQuery, PlayerReliabilitySummaryDto>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly IGameParticipantRepository _gameParticipantRepository;

        public GetPlayerReliabilitySummaryQueryHandler(
            IPlayerProfileRepository playerProfileRepository,
            IGameParticipantRepository gameParticipantRepository)
        {
            _playerProfileRepository = playerProfileRepository;
            _gameParticipantRepository = gameParticipantRepository;
        }

        public async Task<PlayerReliabilitySummaryDto> Handle(
            GetPlayerReliabilitySummaryQuery request,
            CancellationToken cancellationToken)
        {
            var playerProfile = await _playerProfileRepository.GetByIdAsync(
                request.PlayerProfileId,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), request.PlayerProfileId);
            }

            return await _gameParticipantRepository.GetReliabilitySummaryAsync(
                request.PlayerProfileId,
                cancellationToken);
        }
    }
}
