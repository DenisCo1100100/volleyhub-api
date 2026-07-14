using MediatR;
using VolleyHub.Application.Common.Interfaces;
using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Application.PlayerProfiles.Mappings;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfiles
{
    public sealed class GetPlayerProfilesQueryHandler
        : IRequestHandler<GetPlayerProfilesQuery, IReadOnlyList<PlayerProfileDto>>
    {
        private readonly IPlayerProfileRepository _playerProfileRepository;

        public GetPlayerProfilesQueryHandler(IPlayerProfileRepository playerProfileRepository)
        {
            _playerProfileRepository = playerProfileRepository;
        }

        public async Task<IReadOnlyList<PlayerProfileDto>> Handle(
            GetPlayerProfilesQuery request,
            CancellationToken cancellationToken)
        {
            var playerProfiles = await _playerProfileRepository.GetListAsync(cancellationToken);

            return playerProfiles
                .Where(playerProfile => !playerProfile.IsDeleted)
                .Select(playerProfile => playerProfile.ToDto())
                .ToList();
        }
    }
}