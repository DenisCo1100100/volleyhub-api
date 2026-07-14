using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Dtos
{
    public sealed record PlayerProfileDto(
        Guid Id,
        Guid UserId,
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        string? City,
        string? Bio,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}