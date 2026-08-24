using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.Games.Common
{
    public sealed record GameOrganizerSummaryDto(
        Guid Id,
        string DisplayName,
        PlayerSkillLevel SkillLevel);
}