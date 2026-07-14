using MediatR;
using VolleyHub.Application.Common.Exceptions;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Application.PlayerProfiles.Mappings;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById
{
    public sealed class GetPlayerProfileByIdQueryHandler : IRequestHandler<GetPlayerProfileByIdQuery, PlayerProfileDto>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetPlayerProfileByIdQueryHandler(IPlayerProfileRepository playerProfileRepository)
        {
            _playerProfileRepository = playerProfileRepository;
        }

        public async Task<PlayerProfileDto> Handle(
            GetPlayerProfileByIdQuery request,
            CancellationToken cancellationToken)
        {
            var playerProfile = await _playerProfileRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (playerProfile is null || playerProfile.IsDeleted)
            {
                throw new NotFoundException(nameof(PlayerProfile), request.Id);
            }

            return playerProfile.ToDto();
        }
    }
}