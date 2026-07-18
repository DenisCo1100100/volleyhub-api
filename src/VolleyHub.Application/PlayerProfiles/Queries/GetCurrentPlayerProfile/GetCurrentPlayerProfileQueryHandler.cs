using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Application.PlayerProfiles.Mappings;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetCurrentPlayerProfile
{
    public sealed class GetCurrentPlayerProfileQueryHandler
        : IRequestHandler<GetCurrentPlayerProfileQuery, PlayerProfileDto>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetCurrentPlayerProfileQueryHandler(
            IPlayerProfileRepository playerProfileRepository,
            ICurrentUserService currentUserService)
        {
            _playerProfileRepository = playerProfileRepository;
            _currentUserService = currentUserService;
        }

        public async Task<PlayerProfileDto> Handle(
            GetCurrentPlayerProfileQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;

            if (currentUserId is null)
            {
                throw new UnauthorizedException();
            }

            var playerProfile = await _playerProfileRepository.GetByUserIdAsync(
                currentUserId.Value,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), currentUserId.Value);
            }

            return playerProfile.ToDto();
        }
    }
}