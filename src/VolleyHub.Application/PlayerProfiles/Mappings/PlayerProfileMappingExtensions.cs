using VolleyHub.Application.PlayerProfiles.Dtos;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Mappings
{
    public static class PlayerProfileMappingExtensions
    {
        public static PlayerProfileDto ToDto(this PlayerProfile playerProfile)
        {
            return new PlayerProfileDto(
                playerProfile.Id,
                playerProfile.UserId,
                playerProfile.DisplayName,
                playerProfile.SkillLevel,
                playerProfile.City,
                playerProfile.Bio,
                playerProfile.CreatedAt,
                playerProfile.UpdatedAt);
        }
    }
}