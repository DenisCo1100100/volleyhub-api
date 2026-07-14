using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Domain.Games;
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

            var participants = await _gameParticipantRepository.GetByPlayerProfileIdAsync(
                request.PlayerProfileId,
                cancellationToken);

            var attendedGamesCount = participants.Count(
                participant => participant.AttendanceStatus is GameParticipantAttendanceStatus.Present);

            var noShowCount = participants.Count(
                participant => participant.AttendanceStatus is GameParticipantAttendanceStatus.Absent);

            var lateCancellationCount = 0;
            var totalMarkedGamesCount = attendedGamesCount + noShowCount;

            var attendanceRate = totalMarkedGamesCount == 0
                ? 0
                : Math.Round((decimal)attendedGamesCount / totalMarkedGamesCount * 100, 2);

            return new PlayerReliabilitySummaryDto(
                request.PlayerProfileId,
                attendedGamesCount,
                noShowCount,
                lateCancellationCount,
                totalMarkedGamesCount,
                attendanceRate);
        }
    }
}