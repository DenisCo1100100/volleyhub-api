using MediatR;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile
{
    public sealed record UpdatePlayerProfileCommand(
        Guid Id,
        string DisplayName,
        PlayerSkillLevel SkillLevel,
        string? City,
        string? Bio) : IRequest;
}